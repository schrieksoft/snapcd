// Copyright (c) The OpenTofu Authors
// SPDX-License-Identifier: MPL-2.0
// Copyright (c) 2023 HashiCorp, Inc.
// SPDX-License-Identifier: MPL-2.0

package signals

import (
	"github.com/opentofu/opentofu/internal/plans"
)

// ResourceChangesFor reads what a plan does to resources, at every depth. Only
// the changed ones are carried: a reader of an approval is deciding on what the
// plan would do, and a no-op resource is not part of that.
//
// Unlike outputs, these are not filtered to the root module. A root that does
// its work through a child module has no resources of its own, and reporting it
// as unaffected is the thing this exists to prevent. The address carries the
// module path, so where a change lives stays readable.
//
// The change is not decoded, because the address and the action are all that is
// carried and both sit outside the encoded value.
func ResourceChangesFor(plan *plans.Plan) []ResourceChange {
	if plan == nil || plan.Changes == nil {
		return nil
	}

	var out []ResourceChange

	for _, src := range plan.Changes.Resources {
		if src.Action == plans.NoOp {
			continue
		}
		resource := src.Addr.Resource.Resource

		out = append(out, ResourceChange{
			Address: src.Addr.String(),
			Type:    resource.Type,
			Name:    resource.Name,
			Action:  actionString(src.Action.String()),
		})
	}

	return out
}
