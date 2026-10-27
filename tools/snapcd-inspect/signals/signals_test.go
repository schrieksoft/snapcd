package signals

import (
	"encoding/json"
	"testing"

	"github.com/opentofu/opentofu/internal/plans"
	"github.com/zclconf/go-cty/cty"
)

func TestUnknownRoundTrips(t *testing.T) {
	v, err := Encode(plans.Update, cty.StringVal("innocent-ant"), cty.UnknownVal(cty.String))
	if err != nil {
		t.Fatal(err)
	}
	if len(v.Action) != 1 || v.Action[0] != "update" {
		t.Errorf("action = %v, want [update]", v.Action)
	}

	got := through(t, v)
	after, err := got.DecodeAfter()
	if err != nil {
		t.Fatal(err)
	}
	if after.IsKnown() {
		t.Fatalf("after is known after round trip: %#v", after)
	}
	if after.Type() != cty.String {
		t.Errorf("type = %#v, want string", after.Type())
	}

	before, err := got.DecodeBefore()
	if err != nil {
		t.Fatal(err)
	}
	if before.AsString() != "innocent-ant" {
		t.Errorf("before = %q", before.AsString())
	}
}

func TestRefinementsSurvive(t *testing.T) {
	val := cty.UnknownVal(cty.String).Refine().NotNull().StringPrefixFull("subnet-").NewValue()

	v, err := Encode(plans.Update, cty.StringVal("subnet-old"), val)
	if err != nil {
		t.Fatal(err)
	}
	after, err := through(t, v).DecodeAfter()
	if err != nil {
		t.Fatal(err)
	}
	if after.IsKnown() {
		t.Fatal("expected unknown")
	}

	rng := after.Range()
	if rng.StringPrefix() != "subnet-" {
		t.Errorf("string prefix = %q, want %q", rng.StringPrefix(), "subnet-")
	}
	if rng.DefinitelyNotNull() != true {
		t.Error("not-null refinement lost")
	}
}

func TestKnownChangeCarriesValue(t *testing.T) {
	v, err := Encode(plans.Update, cty.StringVal("10.0.1.0/24"), cty.StringVal("10.0.2.0/24"))
	if err != nil {
		t.Fatal(err)
	}
	if len(v.Action) != 1 || v.Action[0] != "update" {
		t.Errorf("action = %v, want [update]", v.Action)
	}
	after, err := through(t, v).DecodeAfter()
	if err != nil {
		t.Fatal(err)
	}
	if !after.IsKnown() || after.AsString() != "10.0.2.0/24" {
		t.Errorf("after = %#v", after)
	}
}

func TestUnchangedIsNoOp(t *testing.T) {
	v, err := Encode(plans.NoOp, cty.StringVal("same"), cty.StringVal("same"))
	if err != nil {
		t.Fatal(err)
	}
	if len(v.Action) != 1 || v.Action[0] != "no-op" {
		t.Errorf("action = %v, want [no-op]", v.Action)
	}
}

func TestCollectionLengthRefinement(t *testing.T) {
	val := cty.UnknownVal(cty.List(cty.String)).Refine().CollectionLengthLowerBound(2).NewValue()

	v, err := Encode(plans.Create, cty.NilVal, val)
	if err != nil {
		t.Fatal(err)
	}
	after, err := through(t, v).DecodeAfter()
	if err != nil {
		t.Fatal(err)
	}
	if got := after.Range().LengthLowerBound(); got != 2 {
		t.Errorf("length lower bound = %d, want 2", got)
	}
}

// through serialises and deserialises a Value as an orchestrator would, to
// prove nothing is lost by passing it through a JSON document.
func through(t *testing.T, v Value) Value {
	t.Helper()
	b, err := json.Marshal(OutputChanges{FormatVersion: FormatVersion, Outputs: map[string]Value{"x": v}})
	if err != nil {
		t.Fatal(err)
	}
	var doc OutputChanges
	if err := json.Unmarshal(b, &doc); err != nil {
		t.Fatal(err)
	}
	return doc.Outputs["x"]
}
