// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

package main

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func writeModule(t *testing.T, files map[string]string) string {
	t.Helper()
	dir := t.TempDir()
	for name, contents := range files {
		if err := os.WriteFile(filepath.Join(dir, name), []byte(contents), 0o644); err != nil {
			t.Fatal(err)
		}
	}
	return dir
}

func variableNamed(t *testing.T, result VariablesResult, name string) VariableInfo {
	t.Helper()
	for _, v := range result.Variables {
		if v.Name == name {
			return v
		}
	}
	t.Fatalf("variable %q not reported; got %+v", name, result.Variables)
	return VariableInfo{}
}

func TestVariableBlocksInsideStringsAreNotReported(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"main.tf": "variable \"real\" {}\n\nresource \"thing\" \"this\" {\n  contents = <<EOT\nvariable \"inside_heredoc\" {}\nEOT\n  quoted = \"variable \\\"inside_quotes\\\" {}\"\n}\n",
	})

	result, err := inspectVariables(dir)
	if err != nil {
		t.Fatal(err)
	}
	if len(result.Variables) != 1 || result.Variables[0].Name != "real" {
		t.Fatalf("expected only \"real\", got %+v", result.Variables)
	}
}

func TestCommentMarkersInsideStringsAreKept(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"variables.tf": "variable \"endpoint\" {\n  description = \"Base URL, e.g. https://example.com/api # not a comment\"\n  default     = \"https://example.com\" // a real comment\n}\n",
	})

	result, err := inspectVariables(dir)
	if err != nil {
		t.Fatal(err)
	}
	v := variableNamed(t, result, "endpoint")
	if v.Description != "Base URL, e.g. https://example.com/api # not a comment" {
		t.Fatalf("description truncated: %q", v.Description)
	}
}

func TestComplexTypeIsReportedAsTagAndDefinition(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"variables.tf": "variable \"settings\" {\n  type = object({\n    names = list(string)\n    sizes = map(number)\n  })\n}\n",
	})

	result, err := inspectVariables(dir)
	if err != nil {
		t.Fatal(err)
	}
	v := variableNamed(t, result, "settings")
	if v.Type != "object" {
		t.Errorf("type tag %q, want %q", v.Type, "object")
	}
	want := `["object",{"names":["list","string"],"sizes":["map","number"]}]`
	if v.TypeDefinition != want {
		t.Errorf("type definition %q, want %q", v.TypeDefinition, want)
	}
}

func TestPrimitiveTypeHasNoDefinition(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"variables.tf": "variable \"name\" {\n  type = string\n}\nvariable \"anything\" {\n  type = any\n}\n",
	})

	result, err := inspectVariables(dir)
	if err != nil {
		t.Fatal(err)
	}
	for name, wantTag := range map[string]string{"name": "string", "anything": "any"} {
		v := variableNamed(t, result, name)
		if v.Type != wantTag {
			t.Errorf("%s: type tag %q, want %q", name, v.Type, wantTag)
		}
		if v.TypeDefinition != "" {
			t.Errorf("%s: type definition %q, want empty", name, v.TypeDefinition)
		}
	}
}

func TestOptionalAttributesSurviveInTheDefinition(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"variables.tf": "variable \"settings\" {\n  type = object({\n    a = string\n    b = optional(bool)\n    c = optional(number, 3)\n  })\n}\n",
	})

	result, err := inspectVariables(dir)
	if err != nil {
		t.Fatal(err)
	}
	want := `["object",{"a":"string","b":"bool","c":"number"},["b","c"]]`
	if got := variableNamed(t, result, "settings").TypeDefinition; got != want {
		t.Errorf("type definition %q, want %q", got, want)
	}
}

func TestDefaultIsReported(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"variables.tf": "variable \"size\" {\n  type    = number\n  default = 3\n}\nvariable \"tags\" {\n  type    = map(string)\n  default = { env = \"dev\" }\n}\nvariable \"required\" {\n  type = string\n}\n",
	})

	result, err := inspectVariables(dir)
	if err != nil {
		t.Fatal(err)
	}
	if got := variableNamed(t, result, "size").Default; got != float64(3) {
		t.Errorf("size default %v (%T), want 3", got, got)
	}
	tags, ok := variableNamed(t, result, "tags").Default.(map[string]interface{})
	if !ok {
		t.Fatalf("tags default is %T, want a map", variableNamed(t, result, "tags").Default)
	}
	if tags["env"] != "dev" {
		t.Errorf("tags default %v, want env=dev", tags)
	}
	if got := variableNamed(t, result, "required").Default; got != nil {
		t.Errorf("required default %v, want nil", got)
	}
}

func TestNullableFollowsExplicitValueThenDefault(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"variables.tf": "variable \"no_default\" {}\nvariable \"with_default\" {\n  default = \"x\"\n}\nvariable \"explicit_false\" {\n  default  = \"x\"\n  nullable = false\n}\nvariable \"explicit_true\" {\n  nullable = true\n}\n",
	})

	result, err := inspectVariables(dir)
	if err != nil {
		t.Fatal(err)
	}
	for name, want := range map[string]bool{"no_default": false, "with_default": true, "explicit_false": false, "explicit_true": true} {
		if got := variableNamed(t, result, name).Nullable; got != want {
			t.Errorf("%s: nullable %v, want %v", name, got, want)
		}
	}
}

func TestSensitiveAndFileAreReported(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"main.tf":            "variable \"plain\" {}\n",
		"extra_variables.tf": "variable \"secret\" {\n  sensitive = true\n}\n",
	})

	result, err := inspectVariables(dir)
	if err != nil {
		t.Fatal(err)
	}
	secret := variableNamed(t, result, "secret")
	if !secret.Sensitive || secret.File != "extra_variables.tf" {
		t.Fatalf("unexpected %+v", secret)
	}
	if plain := variableNamed(t, result, "plain"); plain.Sensitive || plain.File != "main.tf" {
		t.Fatalf("unexpected %+v", plain)
	}
}

func TestTfJsonVariablesAndOutputs(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"main.tf.json": `{"variable": {"region": {"type": "string", "default": "eu"}}, "output": {"id": {"value": "x"}}}`,
	})

	result, err := inspectVariables(dir)
	if err != nil {
		t.Fatal(err)
	}
	region := variableNamed(t, result, "region")
	if region.Type != "string" || !region.Nullable || region.File != "main.tf.json" {
		t.Fatalf("unexpected %+v", region)
	}
	if len(result.Outputs) != 1 || result.Outputs[0].Name != "id" || result.Outputs[0].File != "main.tf.json" {
		t.Fatalf("unexpected outputs %+v", result.Outputs)
	}
}

func TestOutputsAreReportedWithTheirFile(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"outputs.tf":       "output \"a\" {\n  value = 1\n}\n",
		"extra_outputs.tf": "output \"b\" {\n  value = 2\n}\n",
	})

	result, err := inspectVariables(dir)
	if err != nil {
		t.Fatal(err)
	}
	if len(result.Outputs) != 2 || result.Outputs[0].File != "outputs.tf" || result.Outputs[1].File != "extra_outputs.tf" {
		t.Fatalf("unexpected outputs %+v", result.Outputs)
	}
}

func TestDuplicateVariableNamesTheirFiles(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"main.tf":            "variable \"tenant_id\" {}\n",
		"extra_variables.tf": "variable \"tenant_id\" {}\n",
	})

	_, err := inspectVariables(dir)
	if err == nil || !strings.Contains(err.Error(), "extra_variables.tf") || !strings.Contains(err.Error(), "main.tf") {
		t.Fatalf("expected a duplicate error naming both files, got %v", err)
	}
}

func TestOverrideFileIsNotADuplicate(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"main.tf":     "variable \"size\" {\n  default = 1\n}\n",
		"override.tf": "variable \"size\" {\n  nullable = false\n}\n",
	})

	result, err := inspectVariables(dir)
	if err != nil {
		t.Fatal(err)
	}
	if variableNamed(t, result, "size").Nullable {
		t.Fatal("override's nullable = false was not applied")
	}
}

func TestSyntaxErrorIsReported(t *testing.T) {
	dir := writeModule(t, map[string]string{
		"main.tf": "variable \"broken\" {\n",
	})

	if _, err := inspectVariables(dir); err == nil {
		t.Fatal("expected a parse error")
	}
}
