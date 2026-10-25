// Package planproto reads the parts of a saved plan this tool needs.
//
// The fields are decoded off the protobuf wire directly rather than from
// generated code. OpenTofu's own generated package registers "planfile.proto"
// in protobuf's global registry, and a second copy of generated code for the
// same file panics at startup, so a copy cannot be linked alongside it.
//
// Field numbers come from internal/plans/internal/planproto/planfile.proto.
package planproto

import (
	"fmt"

	"google.golang.org/protobuf/encoding/protowire"
)

// Action mirrors planproto.Action. The numbers are part of the wire format.
type Action int32

const (
	Action_NOOP               Action = 0
	Action_CREATE             Action = 1
	Action_READ               Action = 2
	Action_UPDATE             Action = 3
	Action_DELETE             Action = 5
	Action_CREATE_THEN_DELETE Action = 6
	Action_DELETE_THEN_CREATE Action = 7
	Action_FORGET             Action = 8
	Action_FORGET_THEN_CREATE Action = 9
)

func (a Action) String() string {
	switch a {
	case Action_NOOP:
		return "NOOP"
	case Action_CREATE:
		return "CREATE"
	case Action_READ:
		return "READ"
	case Action_UPDATE:
		return "UPDATE"
	case Action_DELETE:
		return "DELETE"
	case Action_CREATE_THEN_DELETE:
		return "CREATE_THEN_DELETE"
	case Action_DELETE_THEN_CREATE:
		return "DELETE_THEN_CREATE"
	case Action_FORGET:
		return "FORGET"
	case Action_FORGET_THEN_CREATE:
		return "FORGET_THEN_CREATE"
	default:
		return fmt.Sprintf("Action(%d)", int32(a))
	}
}

// DynamicValue is a value in cty's own msgpack encoding, which is stable across
// versions. That is what makes reading another build's plan safe here.
type DynamicValue struct {
	Msgpack []byte
}

type Change struct {
	Action Action
	Values []*DynamicValue
}

type OutputChange struct {
	Name      string
	Change    *Change
	Sensitive bool
}

type Plan struct {
	Version       uint64
	OutputChanges []*OutputChange
}

// UnmarshalPlan reads the plan's version and its output changes, ignoring every
// other field.
func UnmarshalPlan(src []byte) (*Plan, error) {
	plan := &Plan{}

	err := eachField(src, func(num protowire.Number, typ protowire.Type, b []byte) error {
		switch {
		case num == 1 && typ == protowire.VarintType:
			v, n := protowire.ConsumeVarint(b)
			if n < 0 {
				return fmt.Errorf("plan version: %w", protowire.ParseError(n))
			}
			plan.Version = v
		case num == 4 && typ == protowire.BytesType:
			raw, n := protowire.ConsumeBytes(b)
			if n < 0 {
				return fmt.Errorf("output change: %w", protowire.ParseError(n))
			}
			oc, err := unmarshalOutputChange(raw)
			if err != nil {
				return err
			}
			plan.OutputChanges = append(plan.OutputChanges, oc)
		}
		return nil
	})

	return plan, err
}

func unmarshalOutputChange(src []byte) (*OutputChange, error) {
	oc := &OutputChange{}

	err := eachField(src, func(num protowire.Number, typ protowire.Type, b []byte) error {
		switch {
		case num == 1 && typ == protowire.BytesType:
			v, n := protowire.ConsumeString(b)
			if n < 0 {
				return fmt.Errorf("output name: %w", protowire.ParseError(n))
			}
			oc.Name = v
		case num == 2 && typ == protowire.BytesType:
			raw, n := protowire.ConsumeBytes(b)
			if n < 0 {
				return fmt.Errorf("output change body: %w", protowire.ParseError(n))
			}
			change, err := unmarshalChange(raw)
			if err != nil {
				return err
			}
			oc.Change = change
		case num == 3 && typ == protowire.VarintType:
			v, n := protowire.ConsumeVarint(b)
			if n < 0 {
				return fmt.Errorf("output sensitive: %w", protowire.ParseError(n))
			}
			oc.Sensitive = v != 0
		}
		return nil
	})

	return oc, err
}

func unmarshalChange(src []byte) (*Change, error) {
	change := &Change{}

	err := eachField(src, func(num protowire.Number, typ protowire.Type, b []byte) error {
		switch {
		case num == 1 && typ == protowire.VarintType:
			v, n := protowire.ConsumeVarint(b)
			if n < 0 {
				return fmt.Errorf("change action: %w", protowire.ParseError(n))
			}
			change.Action = Action(v)
		case num == 2 && typ == protowire.BytesType:
			raw, n := protowire.ConsumeBytes(b)
			if n < 0 {
				return fmt.Errorf("change value: %w", protowire.ParseError(n))
			}
			dv, err := unmarshalDynamicValue(raw)
			if err != nil {
				return err
			}
			change.Values = append(change.Values, dv)
		}
		return nil
	})

	return change, err
}

func unmarshalDynamicValue(src []byte) (*DynamicValue, error) {
	dv := &DynamicValue{}

	err := eachField(src, func(num protowire.Number, typ protowire.Type, b []byte) error {
		if num == 1 && typ == protowire.BytesType {
			v, n := protowire.ConsumeBytes(b)
			if n < 0 {
				return fmt.Errorf("dynamic value: %w", protowire.ParseError(n))
			}
			dv.Msgpack = v
		}
		return nil
	})

	return dv, err
}

// eachField walks a message, handing each field's tag and the remaining bytes to
// fn, then skipping past that field.
func eachField(src []byte, fn func(num protowire.Number, typ protowire.Type, b []byte) error) error {
	for len(src) > 0 {
		num, typ, n := protowire.ConsumeTag(src)
		if n < 0 {
			return protowire.ParseError(n)
		}
		src = src[n:]

		if err := fn(num, typ, src); err != nil {
			return err
		}

		n = protowire.ConsumeFieldValue(num, typ, src)
		if n < 0 {
			return protowire.ParseError(n)
		}
		src = src[n:]
	}
	return nil
}
