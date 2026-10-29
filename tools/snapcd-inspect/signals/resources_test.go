package signals

import (
	"testing"

	"github.com/opentofu/opentofu/internal/addrs"
	"github.com/opentofu/opentofu/internal/plans"
)

func resourceChange(module addrs.ModuleInstance, typeName, name string, action plans.Action) *plans.ResourceInstanceChangeSrc {
	return &plans.ResourceInstanceChangeSrc{
		Addr: module.ResourceInstance(addrs.ManagedResourceMode, typeName, name, addrs.NoKey),
		ChangeSrc: plans.ChangeSrc{
			Action: action,
		},
	}
}

func TestResourceChangesForSkipsNoOp(t *testing.T) {
	plan := &plans.Plan{Changes: &plans.Changes{
		Resources: []*plans.ResourceInstanceChangeSrc{
			resourceChange(addrs.RootModuleInstance, "helm_release", "istio_gateway", plans.Update),
			resourceChange(addrs.RootModuleInstance, "helm_release", "unchanged", plans.NoOp),
		},
	}}

	got := ResourceChangesFor(plan)

	if len(got) != 1 {
		t.Fatalf("want 1 change, got %d: %#v", len(got), got)
	}
	if got[0].Address != "helm_release.istio_gateway" {
		t.Errorf("address: got %q", got[0].Address)
	}
	if got[0].Type != "helm_release" || got[0].Name != "istio_gateway" {
		t.Errorf("type/name: got %q / %q", got[0].Type, got[0].Name)
	}
	if len(got[0].Action) != 1 || got[0].Action[0] != "update" {
		t.Errorf("action: got %#v", got[0].Action)
	}
}

// A root that does its work through a child module has no resources of its own.
// Reporting it as unaffected is what this reader exists to prevent.
func TestResourceChangesForIncludesChildModules(t *testing.T) {
	child := addrs.RootModuleInstance.Child("istio", addrs.NoKey)

	plan := &plans.Plan{Changes: &plans.Changes{
		Resources: []*plans.ResourceInstanceChangeSrc{
			resourceChange(child, "helm_release", "istio_system", plans.DeleteThenCreate),
		},
	}}

	got := ResourceChangesFor(plan)

	if len(got) != 1 {
		t.Fatalf("want 1 change, got %d", len(got))
	}
	if got[0].Address != "module.istio.helm_release.istio_system" {
		t.Errorf("address: got %q", got[0].Address)
	}
	if len(got[0].Action) != 2 || got[0].Action[0] != "delete" || got[0].Action[1] != "create" {
		t.Errorf("action: got %#v", got[0].Action)
	}
}

func TestResourceChangesForNilPlan(t *testing.T) {
	if got := ResourceChangesFor(nil); got != nil {
		t.Errorf("want nil, got %#v", got)
	}
	if got := ResourceChangesFor(&plans.Plan{}); got != nil {
		t.Errorf("want nil for a plan with no changes, got %#v", got)
	}
}
