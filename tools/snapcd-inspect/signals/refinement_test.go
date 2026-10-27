// Copyright (c) The OpenTofu Authors
// SPDX-License-Identifier: MPL-2.0
// Copyright (c) 2023 HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

package signals

import (
	"testing"

	"github.com/opentofu/opentofu/internal/plans"
	"github.com/zclconf/go-cty/cty"
)

// A refinement is what a plan knows about a value it cannot yet state. The
// readable form has to carry it: losing one silently widens what a downstream
// plan believes possible.
func TestRefinedUnknownRoundTrips(t *testing.T) {
	original := cty.UnknownVal(cty.String).Refine().
		NotNull().
		StringPrefix("trusting-").
		NewValue()

	v, err := Encode(plans.Update, cty.StringVal("before"), original)
	if err != nil {
		t.Fatalf("encode: %s", err)
	}

	back, err := v.DecodeAfter()
	if err != nil {
		t.Fatalf("decode: %s", err)
	}

	if back.IsKnown() {
		t.Fatal("expected an unknown")
	}
	if got := back.Range().StringPrefix(); got != "trusting-" {
		t.Fatalf("prefix lost: got %q", got)
	}
	if !back.Range().DefinitelyNotNull() {
		t.Fatal("not-null lost")
	}
}
