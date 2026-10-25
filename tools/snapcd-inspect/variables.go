// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

package main

import (
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"

	"github.com/hashicorp/hcl/v2"
	"github.com/hashicorp/hcl/v2/ext/typeexpr"
	"github.com/hashicorp/hcl/v2/hclparse"
	"github.com/hashicorp/terraform-config-inspect/tfconfig"
	"github.com/zclconf/go-cty/cty"
	ctyjson "github.com/zclconf/go-cty/cty/json"
)

type VariableInfo struct {
	Name string `json:"name"`
	Type string `json:"type,omitempty"`
	// TypeDefinition is the cty JSON encoding of a complex type, in the same form
	// `tofu output -json` reports an output's type. Empty for a primitive type.
	TypeDefinition string      `json:"typeDefinition,omitempty"`
	Default        interface{} `json:"default,omitempty"`
	Description    string      `json:"description,omitempty"`
	Sensitive      bool        `json:"sensitive"`
	Nullable       bool        `json:"nullable"`
	File           string      `json:"file"`
}

type OutputInfo struct {
	Name string `json:"name"`
	File string `json:"file"`
}

type VariablesResult struct {
	Variables []VariableInfo `json:"variables"`
	Outputs   []OutputInfo   `json:"outputs"`
}

var blockSchema = &hcl.BodySchema{
	Blocks: []hcl.BlockHeaderSchema{
		{Type: "variable", LabelNames: []string{"name"}},
		{Type: "output", LabelNames: []string{"name"}},
	},
}

var nullableSchema = &hcl.BodySchema{
	Attributes: []hcl.AttributeSchema{{Name: "nullable"}},
}

var typeSchema = &hcl.BodySchema{
	Attributes: []hcl.AttributeSchema{{Name: "type"}},
}

// typeTag is the leading identifier of a type expression: "object" for
// object({...}), "string" for string. Empty when the expression is not a type.
func typeTag(expr hcl.Expression) string {
	ty, _, diags := typeexpr.TypeConstraintWithDefaults(expr)
	if diags.HasErrors() {
		return ""
	}
	switch {
	case ty.IsObjectType():
		return "object"
	case ty.IsTupleType():
		return "tuple"
	case ty.IsMapType():
		return "map"
	case ty.IsListType():
		return "list"
	case ty.IsSetType():
		return "set"
	case ty == cty.DynamicPseudoType:
		return "any"
	default:
		return ty.FriendlyName()
	}
}

// typeDefinition is the cty JSON encoding of a type expression, returned only for
// a complex type: a primitive is fully described by its tag alone.
func typeDefinition(expr hcl.Expression) (string, error) {
	ty, _, diags := typeexpr.TypeConstraintWithDefaults(expr)
	if diags.HasErrors() {
		return "", diags
	}
	if ty.IsPrimitiveType() || ty == cty.DynamicPseudoType {
		return "", nil
	}
	encoded, err := ctyjson.MarshalType(ty)
	if err != nil {
		return "", err
	}
	return string(encoded), nil
}

// configFiles lists a module directory's configuration files the way OpenTofu loads them: primary files first, then overrides.
func configFiles(dir string) (primary []string, override []string, err error) {
	entries, err := os.ReadDir(dir)
	if err != nil {
		return nil, nil, err
	}

	for _, entry := range entries {
		name := entry.Name()
		if entry.IsDir() || strings.HasPrefix(name, ".") || strings.HasSuffix(name, "~") || (strings.HasPrefix(name, "#") && strings.HasSuffix(name, "#")) {
			continue
		}

		var base string
		switch {
		case strings.HasSuffix(name, ".tf.json"):
			base = strings.TrimSuffix(name, ".tf.json")
		case strings.HasSuffix(name, ".tf"):
			base = strings.TrimSuffix(name, ".tf")
		default:
			continue
		}

		if base == "override" || strings.HasSuffix(base, "_override") {
			override = append(override, name)
		} else {
			primary = append(primary, name)
		}
	}

	sort.Strings(primary)
	sort.Strings(override)
	return primary, override, nil
}

func parseFile(parser *hclparse.Parser, path string) (*hcl.File, hcl.Diagnostics) {
	if strings.HasSuffix(path, ".tf.json") {
		return parser.ParseJSONFile(path)
	}
	return parser.ParseHCLFile(path)
}

// inspectVariables reads the variable and output declarations of the module in dir.
func inspectVariables(dir string) (VariablesResult, error) {
	primary, override, err := configFiles(dir)
	if err != nil {
		return VariablesResult{}, err
	}

	parser := hclparse.NewParser()
	variableFile := map[string]string{}
	outputFile := map[string]string{}
	explicitNullable := map[string]bool{}
	variableTypeTag := map[string]string{}
	variableTypeDefinition := map[string]string{}
	var problems []string

	read := func(name string, isOverride bool) {
		file, diags := parseFile(parser, filepath.Join(dir, name))
		if diags.HasErrors() {
			problems = append(problems, diags.Error())
			return
		}

		content, _, diags := file.Body.PartialContent(blockSchema)
		if diags.HasErrors() {
			problems = append(problems, diags.Error())
			return
		}

		for _, block := range content.Blocks {
			label := block.Labels[0]
			switch block.Type {
			case "variable":
				if previous, seen := variableFile[label]; seen && !isOverride {
					problems = append(problems, fmt.Sprintf("variable %q is declared in both %s and %s", label, previous, name))
					continue
				}
				if !isOverride {
					variableFile[label] = name
				}
				attrs, _, _ := block.Body.PartialContent(nullableSchema)
				if attr, ok := attrs.Attributes["nullable"]; ok {
					value, valueDiags := attr.Expr.Value(nil)
					if valueDiags.HasErrors() || value.Type() != cty.Bool || value.IsNull() {
						problems = append(problems, fmt.Sprintf("variable %q in %s: nullable must be true or false", label, name))
						continue
					}
					explicitNullable[label] = value.True()
				}

				typeAttrs, _, _ := block.Body.PartialContent(typeSchema)
				if attr, ok := typeAttrs.Attributes["type"]; ok {
					definition, defErr := typeDefinition(attr.Expr)
					if defErr != nil {
						problems = append(problems, fmt.Sprintf("variable %q in %s: %s", label, name, defErr))
						continue
					}
					variableTypeTag[label] = typeTag(attr.Expr)
					variableTypeDefinition[label] = definition
				}
			case "output":
				if previous, seen := outputFile[label]; seen && !isOverride {
					problems = append(problems, fmt.Sprintf("output %q is declared in both %s and %s", label, previous, name))
					continue
				}
				if !isOverride {
					outputFile[label] = name
				}
			}
		}
	}

	for _, name := range primary {
		read(name, false)
	}
	for _, name := range override {
		read(name, true)
	}

	if len(problems) > 0 {
		return VariablesResult{}, fmt.Errorf("%s", strings.Join(problems, "\n"))
	}

	module, diags := tfconfig.LoadModule(dir)
	if diags.HasErrors() {
		return VariablesResult{}, diags.Err()
	}

	result := VariablesResult{Variables: []VariableInfo{}, Outputs: []OutputInfo{}}
	for name, variable := range module.Variables {
		// Nullable: the explicit value when set, else true for a variable with a default.
		nullable, explicit := explicitNullable[name]
		if !explicit {
			nullable = !variable.Required
		}

		// The tag comes from the parsed type expression; tfconfig's own Type is the
		// source text, which carries every attribute of an object type.
		tag, parsed := variableTypeTag[name]
		if !parsed {
			tag = variable.Type
		}

		result.Variables = append(result.Variables, VariableInfo{
			Name:           name,
			Type:           tag,
			TypeDefinition: variableTypeDefinition[name],
			Default:        variable.Default,
			Description:    variable.Description,
			Sensitive:      variable.Sensitive,
			Nullable:       nullable,
			File:           variableFile[name],
		})
	}
	for name := range module.Outputs {
		result.Outputs = append(result.Outputs, OutputInfo{Name: name, File: outputFile[name]})
	}

	sort.Slice(result.Variables, func(a, b int) bool { return result.Variables[a].Name < result.Variables[b].Name })
	sort.Slice(result.Outputs, func(a, b int) bool { return result.Outputs[a].Name < result.Outputs[b].Name })
	return result, nil
}
