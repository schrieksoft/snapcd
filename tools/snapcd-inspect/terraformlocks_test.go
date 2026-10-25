package main

import (
	"testing"

	"github.com/opentofu/opentofu/internal/addrs"
	"github.com/opentofu/opentofu/internal/depsfile"
	"github.com/opentofu/opentofu/internal/getproviders"
)

func provider(t *testing.T, host, namespace, typ string) addrs.Provider {
	t.Helper()
	p, diags := addrs.ParseProviderSourceString(host + "/" + namespace + "/" + typ)
	if diags.HasErrors() {
		t.Fatalf("parse %s/%s/%s: %s", host, namespace, typ, diags.Err())
	}
	return p
}

func locksWith(t *testing.T, addr addrs.Provider) *depsfile.Locks {
	t.Helper()
	locks := depsfile.NewLocks()
	locks.SetProvider(
		addr,
		getproviders.MustParseVersion("3.9.1"),
		getproviders.MustParseVersionConstraints(">= 3.0.0"),
		[]getproviders.Hash{getproviders.MustParseHash("h1:abc123")},
	)
	return locks
}

// A provider Terraform recorded is re-keyed to the engine's registry, and the
// package is still looked for where Terraform put it.
func TestRetargetLocksRekeysTerraformProviders(t *testing.T) {
	onDiskAddr := provider(t, terraformRegistryHost, "hashicorp", "random")

	retargeted, onDisk := retargetLocks(locksWith(t, onDiskAddr))

	want := provider(t, addrs.DefaultProviderRegistryHost.ForDisplay(), "hashicorp", "random")

	lock := retargeted.Provider(want)
	if lock == nil {
		t.Fatalf("no lock keyed to %s; got %v", want, keys(retargeted))
	}
	if got := lock.Version().String(); got != "3.9.1" {
		t.Errorf("version = %s, want 3.9.1", got)
	}
	if len(lock.AllHashes()) != 1 {
		t.Errorf("hashes = %d, want 1; the checksum check needs them carried over", len(lock.AllHashes()))
	}
	if got := onDisk[want]; got != onDiskAddr {
		t.Errorf("onDisk[%s] = %s, want %s", want, got, onDiskAddr)
	}
}

// A directory the engine itself initialised is left exactly as it was.
func TestRetargetLocksLeavesOwnProvidersAlone(t *testing.T) {
	addr := provider(t, addrs.DefaultProviderRegistryHost.ForDisplay(), "hashicorp", "random")
	locks := locksWith(t, addr)

	retargeted, onDisk := retargetLocks(locks)

	if retargeted != locks {
		t.Error("locks were replaced; a non-Terraform directory must be passed through untouched")
	}
	if onDisk != nil {
		t.Errorf("onDisk = %v, want nil so providerFactories uses the address as given", onDisk)
	}
}

// A source carrying an explicit third-party host keeps that host: the
// configuration, the lock file and the cache already agree on it.
func TestRetargetLocksKeepsExplicitThirdPartyHost(t *testing.T) {
	addr := provider(t, "registry.example.com", "acme", "widget")

	retargeted, _ := retargetLocks(locksWith(t, addr))

	if lock := retargeted.Provider(addr); lock == nil {
		t.Errorf("no lock keyed to %s; got %v", addr, keys(retargeted))
	}
}

func keys(locks *depsfile.Locks) []addrs.Provider {
	var out []addrs.Provider
	for addr := range locks.AllProviders() {
		out = append(out, addr)
	}
	return out
}
