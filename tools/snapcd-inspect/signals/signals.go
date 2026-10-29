// Package signals holds the wire format passed between roots.
//
// A value may be unknown, which is the whole point: an unknown carries the fact
// that a value will change without claiming what it will change to. A known
// value is written as itself, so a document can be read, logged and diffed
// without decoding anything. An unknown has no JSON scalar, so it is written as
// an object carrying "$unknown", with whatever the plan has narrowed it down to
// alongside: nullness, a string prefix, number or length bounds.
//
// Callers that route these between roots can move a Value verbatim, and can
// also read it.
package signals

import (
	"encoding/json"
	"fmt"

	"github.com/opentofu/opentofu/internal/plans"
	"github.com/zclconf/go-cty/cty"
	ctyjson "github.com/zclconf/go-cty/cty/json"
)

// Action is the plan's own action for the value, in the same form "tofu show
// -json" writes it: one element for a simple action and two for a replace.
type Action []string

// Value is one output change or one variable value.
//
// Action is the plan's own verdict where one is available, so a destroyed
// output reads as a delete rather than as an update to null.
type Value struct {
	Action Action `json:"action"`

	// Type is the value's cty type as ctyjson writes it, which is already JSON:
	// carried as a raw message so it reads as "string" rather than base64.
	Type json.RawMessage `json:"type"`

	// Before and After are the value itself where the plan knows it, and an
	// object carrying "$unknown" where it does not. Readable on purpose: a wave
	// is diagnosed by reading these.
	Before json.RawMessage `json:"before,omitempty"`
	After  json.RawMessage `json:"after,omitempty"`

	// Sensitive marks a value the upstream root declared sensitive. It is
	// carried so a consumer can avoid printing it; the encoded value is not
	// itself protected.
	Sensitive bool `json:"sensitive,omitempty"`
}

// ResourceChange is one resource the plan acts on, carried so an approver can
// read what a probe would do to this root rather than only what it passes on.
//
// No attribute values: a resource's attributes are large and often secret,
// where an output's value is small and has a sensitive path of its own. The
// address and the verb are what the reader of an approval needs.
type ResourceChange struct {
	// Address is the resource's address in the root, as tofu prints it.
	Address string `json:"address"`

	Type string `json:"type"`
	Name string `json:"name"`

	// Action is the plan's verdict, in the same wire form as a Value's.
	Action Action `json:"action"`
}

// OutputChanges is what a root emits: keyed by output name.
type OutputChanges struct {
	FormatVersion string           `json:"format_version"`
	Outputs       map[string]Value `json:"outputs"`

	// ResourceChanges is what the plan does to this root's own resources, and
	// is absent on a document built from a plan file's outputs alone.
	ResourceChanges []ResourceChange `json:"resource_changes,omitempty"`
}

// Inputs is what a root consumes: keyed by variable name. The orchestrator
// builds this from one or more OutputChanges by renaming keys according to the
// dependency graph, which only it knows.
type Inputs struct {
	FormatVersion string           `json:"format_version"`
	Variables     map[string]Value `json:"variables"`
}

const FormatVersion = "1"

// Encode builds a Value from a before/after pair. Either may be cty.NilVal to
// mean absent.
// Encode builds a Value from a plan's own output change. The action is the
// plan's verdict rather than something inferred from the two values, so a
// destroyed output reads as a delete rather than as an update to null.
func Encode(action plans.Action, before, after cty.Value) (Value, error) {
	// A destroyed output arrives as a null of no particular type, so the type
	// has to come from whichever side actually declares one.
	ty := cty.DynamicPseudoType
	switch {
	case after != cty.NilVal && after.Type() != cty.DynamicPseudoType:
		ty = after.Type()
	case before != cty.NilVal && before.Type() != cty.DynamicPseudoType:
		ty = before.Type()
	case after != cty.NilVal:
		ty = after.Type()
	case before != cty.NilVal:
		ty = before.Type()
	}

	tyJSON, err := ctyjson.MarshalType(ty)
	if err != nil {
		return Value{}, fmt.Errorf("marshal type: %w", err)
	}

	v := Value{Action: actionString(action.String()), Type: tyJSON}

	if before != cty.NilVal {
		if v.Before, err = encodeValue(before, ty); err != nil {
			return Value{}, fmt.Errorf("marshal before: %w", err)
		}
	}
	if after != cty.NilVal {
		if v.After, err = encodeValue(after, ty); err != nil {
			return Value{}, fmt.Errorf("marshal after: %w", err)
		}
	}
	return v, nil
}

// DecodeAfter returns the planned new value, which is what a downstream root
// needs. An absent After decodes as an unknown of the declared type, since a
// value the upstream plan could not predict is exactly an unknown.
func (v Value) DecodeAfter() (cty.Value, error) {
	ty, err := v.decodeType()
	if err != nil {
		return cty.NilVal, err
	}
	if len(v.After) == 0 {
		return cty.UnknownVal(ty), nil
	}
	return decodeValue(v.After, ty)
}

// DecodeBefore returns the prior value, or cty.NilVal if there was none.
func (v Value) DecodeBefore() (cty.Value, error) {
	if len(v.Before) == 0 {
		return cty.NilVal, nil
	}
	ty, err := v.decodeType()
	if err != nil {
		return cty.NilVal, err
	}
	return decodeValue(v.Before, ty)
}

func (v Value) decodeType() (cty.Type, error) {
	ty, err := ctyjson.UnmarshalType(v.Type)
	if err != nil {
		return cty.NilType, fmt.Errorf("unmarshal type: %w", err)
	}
	return ty, nil
}

func encodeValue(val cty.Value, ty cty.Type) (json.RawMessage, error) {
	return readableValue(val, ty)
}

func decodeValue(raw json.RawMessage, ty cty.Type) (cty.Value, error) {
	val, err := fromReadable(raw, ty)
	if err != nil {
		return cty.NilVal, fmt.Errorf("read value: %w", err)
	}
	return val, nil
}

// ReadInputs parses an Inputs document.
func ReadInputs(b []byte) (*Inputs, error) {
	var in Inputs
	if err := json.Unmarshal(b, &in); err != nil {
		return nil, fmt.Errorf("parse inputs: %w", err)
	}
	if in.FormatVersion != "" && in.FormatVersion != FormatVersion {
		return nil, fmt.Errorf("unsupported format_version %q, want %q", in.FormatVersion, FormatVersion)
	}
	if in.Variables == nil {
		in.Variables = map[string]Value{}
	}
	return &in, nil
}
