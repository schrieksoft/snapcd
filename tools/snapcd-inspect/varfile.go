package main

// Reads a variable file the way `tofu -var-file` does.
//
// OpenTofu does this in Meta.addVarsFromFile, which is unexported, so the shape
// is reproduced: parse the file as HCL (or JSON, by extension), take its
// top-level attributes, and present each as a value the plan can take.

import (
	"fmt"
	"strings"

	"github.com/hashicorp/hcl/v2"
	"github.com/hashicorp/hcl/v2/hclparse"

	"github.com/opentofu/opentofu/internal/backend"
	"github.com/opentofu/opentofu/internal/configs"
	"github.com/opentofu/opentofu/internal/tfdiags"
	"github.com/opentofu/opentofu/internal/tofu"
)

// fileVariableValue is one attribute of a variable file, evaluated on demand.
type fileVariableValue struct {
	expr hcl.Expression
}

func (v fileVariableValue) ParseVariableValue(_ configs.VariableParsingMode) (*tofu.InputValue, tfdiags.Diagnostics) {
	var diags tfdiags.Diagnostics

	val, hclDiags := v.expr.Value(nil)
	diags = diags.Append(hclDiags)

	return &tofu.InputValue{
		Value:      val,
		SourceType: tofu.ValueFromNamedFile,
	}, diags
}

// varsFromFile reads filename and returns a value per variable it sets.
func varsFromFile(filename string) (map[string]backend.UnparsedVariableValue, error) {
	values := map[string]backend.UnparsedVariableValue{}
	if filename == "" {
		return values, nil
	}

	parser := hclparse.NewParser()

	var file *hcl.File
	var diags hcl.Diagnostics
	if strings.HasSuffix(filename, ".json") {
		file, diags = parser.ParseJSONFile(filename)
	} else {
		file, diags = parser.ParseHCLFile(filename)
	}
	if diags.HasErrors() {
		return nil, fmt.Errorf("read %s: %s", filename, diags.Error())
	}

	attrs, hclDiags := file.Body.JustAttributes()
	if hclDiags.HasErrors() {
		return nil, fmt.Errorf("read %s: %s", filename, hclDiags.Error())
	}

	for name, attr := range attrs {
		values[name] = fileVariableValue{expr: attr.Expr}
	}

	return values, nil
}
