// Copyright (c) The OpenTofu Authors
// SPDX-License-Identifier: MPL-2.0
// Copyright (c) 2023 HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

package signals

import (
	"fmt"

	"github.com/opentofu/opentofu/internal/configs"
	"github.com/opentofu/opentofu/internal/tfdiags"
	"github.com/opentofu/opentofu/internal/tofu"
)

// PredictedValue is one variable value an upstream root's plan predicts,
// presented as a variable value the plan can consume.
//
// It satisfies backend.UnparsedVariableValue, so a predicted value takes part
// in the same merge as every other source. The value is already a cty value,
// decoded from the signals document, so parsing does nothing but hand it over:
// an unknown has survived the trip encoded and must not be re-parsed as text.
type PredictedValue struct {
	Name  string
	Value Value
}

func (p PredictedValue) ParseVariableValue(_ configs.VariableParsingMode) (*tofu.InputValue, tfdiags.Diagnostics) {
	var diags tfdiags.Diagnostics

	val, err := p.Value.DecodeAfter()
	if err != nil {
		diags = diags.Append(tfdiags.Sourceless(
			tfdiags.Error,
			"Invalid predicted variable value",
			fmt.Sprintf("The predicted value for variable %q could not be decoded: %s.", p.Name, err),
		))
		return nil, diags
	}

	// ValueFromCLIArg rather than a source of its own: a predicted value is
	// supplied by the caller for this one run, which is what that source means
	// to every error message and precedence check downstream.
	return &tofu.InputValue{
		Value:      val,
		SourceType: tofu.ValueFromCLIArg,
	}, diags
}
