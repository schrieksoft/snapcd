package planread

// Turns a decoded output change into the shape the rest of the tool reads.
//
// The action-to-value-slot mapping is OpenTofu's, from
// internal/plans/planfile/tfplan.go. An output change carries only an action and
// its before and after values: the importing, identity, generated-config and
// sensitive-path fields a resource change has never appear on one.

import (
	"fmt"

	"github.com/opentofu/opentofu/internal/plans"

	"github.com/opentofu/opentofu/snapcdinspect/planproto"
)

func changeFromTfplan(rawChange *planproto.Change) (*plans.ChangeSrc, error) {
	if rawChange == nil {
		return nil, fmt.Errorf("change is absent")
	}

	ret := &plans.ChangeSrc{}

	// Which of the values slots hold the before and after, per action. -1 means
	// the action has no value in that position.
	beforeIdx, afterIdx := -1, -1

	switch rawChange.Action {
	case planproto.Action_NOOP:
		ret.Action = plans.NoOp
		beforeIdx, afterIdx = 0, 0
	case planproto.Action_CREATE:
		ret.Action = plans.Create
		afterIdx = 0
	case planproto.Action_READ:
		ret.Action = plans.Read
		beforeIdx, afterIdx = 0, 1
	case planproto.Action_UPDATE:
		ret.Action = plans.Update
		beforeIdx, afterIdx = 0, 1
	case planproto.Action_DELETE:
		ret.Action = plans.Delete
		beforeIdx = 0
	case planproto.Action_FORGET:
		ret.Action = plans.Forget
		beforeIdx = 0
	case planproto.Action_CREATE_THEN_DELETE:
		ret.Action = plans.CreateThenDelete
		beforeIdx, afterIdx = 0, 1
	case planproto.Action_DELETE_THEN_CREATE:
		ret.Action = plans.DeleteThenCreate
		beforeIdx, afterIdx = 0, 1
	case planproto.Action_FORGET_THEN_CREATE:
		ret.Action = plans.ForgetThenCreate
		afterIdx = 0
	default:
		return nil, fmt.Errorf("invalid change action %s", rawChange.Action)
	}

	if beforeIdx != -1 {
		if l := len(rawChange.Values); l <= beforeIdx {
			return nil, fmt.Errorf("incorrect number of values (%d) for %s change", l, rawChange.Action)
		}
		ret.Before = valueFromTfplan(rawChange.Values[beforeIdx])
	}

	if afterIdx != -1 {
		if l := len(rawChange.Values); l <= afterIdx {
			return nil, fmt.Errorf("incorrect number of values (%d) for %s change", l, rawChange.Action)
		}
		ret.After = valueFromTfplan(rawChange.Values[afterIdx])
	}

	return ret, nil
}

// valueFromTfplan hands over the value in cty's own msgpack encoding, which is
// what plans.DynamicValue holds and what makes it readable across versions.
func valueFromTfplan(rawV *planproto.DynamicValue) plans.DynamicValue {
	if rawV == nil {
		return nil
	}
	return plans.DynamicValue(rawV.Msgpack)
}
