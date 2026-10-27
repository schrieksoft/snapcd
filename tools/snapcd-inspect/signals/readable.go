// Copyright (c) The OpenTofu Authors
// SPDX-License-Identifier: MPL-2.0
// Copyright (c) 2023 HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

package signals

import (
	"encoding/json"
	"fmt"
	"math/big"

	"github.com/zclconf/go-cty/cty"
	ctyjson "github.com/zclconf/go-cty/cty/json"
)

// unknownMarker is the member that tells a known value from one a plan cannot
// predict. A known value is its own JSON; JSON has no scalar for an unknown, so
// one is written as an object carrying this.
const unknownMarker = "$unknown"

// Refinement is what a plan knows about a value it cannot yet state: that it
// will not be null, that it starts with a given prefix, or that it falls within
// bounds. cty offers exactly these, and each is plain data.
type Refinement struct {
	NotNull bool `json:"not_null,omitempty"`
	Null    bool `json:"null,omitempty"`

	Prefix string `json:"prefix,omitempty"`

	NumberMin          *json.Number `json:"number_min,omitempty"`
	NumberMinInclusive bool         `json:"number_min_inclusive,omitempty"`
	NumberMax          *json.Number `json:"number_max,omitempty"`
	NumberMaxInclusive bool         `json:"number_max_inclusive,omitempty"`

	LengthMin *int `json:"length_min,omitempty"`
	LengthMax *int `json:"length_max,omitempty"`
}

// readableValue renders a value as JSON a person can read: the value itself
// when the plan knows it, and a marked object when it does not.
func readableValue(val cty.Value, ty cty.Type) (json.RawMessage, error) {
	if val.IsWhollyKnown() {
		// Marshal handles every known value, structured ones included, and is
		// the only encoder involved for them.
		b, err := ctyjson.Marshal(val, ty)
		if err != nil {
			return nil, err
		}
		return b, nil
	}

	// A container can be known while some element inside it is not, so the
	// unknown has to be marked where it sits rather than swallowing the whole
	// value.
	if val.IsKnown() && !val.IsNull() && (ty.IsListType() || ty.IsSetType() || ty.IsTupleType()) {
		return readableSequence(val, ty)
	}
	if val.IsKnown() && !val.IsNull() && (ty.IsMapType() || ty.IsObjectType()) {
		return readableMapping(val, ty)
	}

	marked := map[string]any{unknownMarker: true}
	if r := refinementOf(val); r != nil {
		marked["refinement"] = r
	}

	b, err := json.Marshal(marked)
	if err != nil {
		return nil, err
	}
	return b, nil
}

// readableSequence encodes a list, set or tuple element by element, so an
// unknown element is marked in place.
func readableSequence(val cty.Value, ty cty.Type) (json.RawMessage, error) {
	elems := make([]json.RawMessage, 0, val.LengthInt())
	for it := val.ElementIterator(); it.Next(); {
		_, ev := it.Element()
		raw, err := readableValue(ev, ev.Type())
		if err != nil {
			return nil, err
		}
		elems = append(elems, raw)
	}
	return json.Marshal(elems)
}

// readableMapping encodes a map or object attribute by attribute, so an
// unknown value is marked in place.
func readableMapping(val cty.Value, ty cty.Type) (json.RawMessage, error) {
	attrs := make(map[string]json.RawMessage, val.LengthInt())
	for it := val.ElementIterator(); it.Next(); {
		k, ev := it.Element()
		raw, err := readableValue(ev, ev.Type())
		if err != nil {
			return nil, err
		}
		attrs[k.AsString()] = raw
	}
	return json.Marshal(attrs)
}

// refinementOf reads whatever a plan has narrowed an unknown down to, or nil
// when it has narrowed nothing.
func refinementOf(val cty.Value) *Refinement {
	rng := val.Range()
	ty := val.Type()

	var r Refinement
	var any bool

	if rng.DefinitelyNotNull() {
		r.NotNull = true
		any = true
	}

	if ty == cty.String {
		if prefix := rng.StringPrefix(); prefix != "" {
			r.Prefix = prefix
			any = true
		}
	}

	if ty == cty.Number {
		if min, inclusive := rng.NumberLowerBound(); min.IsKnown() && !min.IsNull() {
			n := json.Number(min.AsBigFloat().Text('f', -1))
			r.NumberMin = &n
			r.NumberMinInclusive = inclusive
			any = true
		}
		if max, inclusive := rng.NumberUpperBound(); max.IsKnown() && !max.IsNull() {
			n := json.Number(max.AsBigFloat().Text('f', -1))
			r.NumberMax = &n
			r.NumberMaxInclusive = inclusive
			any = true
		}
	}

	if ty.IsCollectionType() {
		if min := rng.LengthLowerBound(); min > 0 {
			r.LengthMin = &min
			any = true
		}
		if max := rng.LengthUpperBound(); max < cty.UnknownVal(ty).Range().LengthUpperBound() {
			r.LengthMax = &max
			any = true
		}
	}

	if !any {
		return nil
	}
	return &r
}

// fromReadable turns a readable value back into a cty value, rebuilding an
// unknown and its refinements from the marked form.
func fromReadable(raw json.RawMessage, ty cty.Type) (cty.Value, error) {
	var probe map[string]json.RawMessage
	if err := json.Unmarshal(raw, &probe); err == nil {
		if _, marked := probe[unknownMarker]; marked {
			return unknownFromReadable(probe, ty)
		}
	}

	// An unknown may sit anywhere inside a container, so each element is
	// decoded on its own rather than handing the whole thing to the cty
	// decoder, which has no marker of its own for an unknown.
	switch {
	case ty.IsListType(), ty.IsSetType(), ty.IsTupleType():
		if v, ok, err := sequenceFromReadable(raw, ty); ok {
			return v, err
		}
	case ty.IsMapType(), ty.IsObjectType():
		if v, ok, err := mappingFromReadable(raw, ty); ok {
			return v, err
		}
	}

	return ctyjson.Unmarshal(raw, ty)
}

// sequenceFromReadable decodes a list, set or tuple whose elements may be
// marked unknown. ok is false when raw is not a JSON array, which leaves the
// cty decoder to report the mismatch.
func sequenceFromReadable(raw json.RawMessage, ty cty.Type) (cty.Value, bool, error) {
	var rawElems []json.RawMessage
	if err := json.Unmarshal(raw, &rawElems); err != nil {
		return cty.NilVal, false, nil
	}

	if len(rawElems) == 0 {
		return cty.NilVal, false, nil
	}

	elems := make([]cty.Value, 0, len(rawElems))
	for i, rawElem := range rawElems {
		var elemTy cty.Type
		if ty.IsTupleType() {
			if i >= ty.Length() {
				return cty.NilVal, true, fmt.Errorf("too many elements for %s", ty.FriendlyName())
			}
			elemTy = ty.TupleElementType(i)
		} else {
			elemTy = ty.ElementType()
		}
		v, err := fromReadable(rawElem, elemTy)
		if err != nil {
			return cty.NilVal, true, err
		}
		elems = append(elems, v)
	}

	switch {
	case ty.IsSetType():
		return cty.SetVal(elems), true, nil
	case ty.IsTupleType():
		return cty.TupleVal(elems), true, nil
	default:
		return cty.ListVal(elems), true, nil
	}
}

// mappingFromReadable decodes a map or object whose values may be marked
// unknown. ok is false when raw is not a JSON object.
func mappingFromReadable(raw json.RawMessage, ty cty.Type) (cty.Value, bool, error) {
	var rawAttrs map[string]json.RawMessage
	if err := json.Unmarshal(raw, &rawAttrs); err != nil {
		return cty.NilVal, false, nil
	}

	if len(rawAttrs) == 0 {
		return cty.NilVal, false, nil
	}

	attrs := make(map[string]cty.Value, len(rawAttrs))
	for k, rawAttr := range rawAttrs {
		var attrTy cty.Type
		if ty.IsObjectType() {
			if !ty.HasAttribute(k) {
				return cty.NilVal, true, fmt.Errorf("unexpected attribute %q", k)
			}
			attrTy = ty.AttributeType(k)
		} else {
			attrTy = ty.ElementType()
		}
		v, err := fromReadable(rawAttr, attrTy)
		if err != nil {
			return cty.NilVal, true, err
		}
		attrs[k] = v
	}

	if ty.IsObjectType() {
		return cty.ObjectVal(attrs), true, nil
	}
	return cty.MapVal(attrs), true, nil
}

func unknownFromReadable(probe map[string]json.RawMessage, ty cty.Type) (cty.Value, error) {
	val := cty.UnknownVal(ty)

	rawRefinement, ok := probe["refinement"]
	if !ok {
		return val, nil
	}

	var r Refinement
	if err := json.Unmarshal(rawRefinement, &r); err != nil {
		return cty.NilVal, fmt.Errorf("parse refinement: %w", err)
	}

	b := val.Refine()

	switch {
	case r.Null:
		b = b.Null()
	case r.NotNull:
		b = b.NotNull()
	}

	if r.Prefix != "" && ty == cty.String {
		b = b.StringPrefix(r.Prefix)
	}

	if ty == cty.Number {
		if r.NumberMin != nil {
			if f, _, err := big.ParseFloat(r.NumberMin.String(), 10, 512, big.ToNearestEven); err == nil {
				b = b.NumberRangeLowerBound(cty.NumberVal(f), r.NumberMinInclusive)
			}
		}
		if r.NumberMax != nil {
			if f, _, err := big.ParseFloat(r.NumberMax.String(), 10, 512, big.ToNearestEven); err == nil {
				b = b.NumberRangeUpperBound(cty.NumberVal(f), r.NumberMaxInclusive)
			}
		}
	}

	if ty.IsCollectionType() {
		if r.LengthMin != nil {
			b = b.CollectionLengthLowerBound(*r.LengthMin)
		}
		if r.LengthMax != nil {
			b = b.CollectionLengthUpperBound(*r.LengthMax)
		}
	}

	return b.NewValue(), nil
}
