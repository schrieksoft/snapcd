package main

// Lets a probe run in a directory that Terraform initialised.
//
// A provider source written without a hostname, such as "hashicorp/random",
// resolves against the tool's own default registry. Terraform resolves it to
// registry.terraform.io and records that in the lock file and the cache path;
// OpenTofu resolves it to registry.opentofu.org. The two therefore disagree
// about the provider's identity even though the binary behind it is the same
// release from the same upstream.
//
// The engine compares the lock file against what the configuration requires
// before anything else runs, so the locks it is given must use the hostname it
// resolves to. The provider packages, meanwhile, sit on disk under the hostname
// Terraform wrote. So the locks are re-keyed to the engine's hostname and the
// original address is kept to find the package.

import (
	"github.com/opentofu/opentofu/internal/addrs"
	"github.com/opentofu/opentofu/internal/depsfile"
	"github.com/opentofu/svchost"
)

// terraformRegistryHost is the hostname Terraform resolves an unqualified
// provider source to.
const terraformRegistryHost = "registry.terraform.io"

// providerAddrMap says where to find the package for a provider the engine asks
// for, when the two disagree about the hostname.
type providerAddrMap map[addrs.Provider]addrs.Provider

// retargetLocks returns the locks with every provider that Terraform recorded
// re-keyed to the engine's own default registry, plus a map from the re-keyed
// address back to the original so the package can still be found on disk.
//
// A provider with an explicit hostname in its source is left alone: the
// configuration, the lock file and the cache all already agree on it.
func retargetLocks(locks *depsfile.Locks) (*depsfile.Locks, providerAddrMap) {
	tfHost, err := svchost.ForComparison(terraformRegistryHost)
	if err != nil {
		return locks, nil
	}

	retargeted := depsfile.NewLocks()
	onDisk := providerAddrMap{}
	changed := false

	for addr, lock := range locks.AllProviders() {
		target := addr
		if addr.Hostname == tfHost {
			target = addrs.Provider{
				Type:      addr.Type,
				Namespace: addr.Namespace,
				Hostname:  addrs.DefaultProviderRegistryHost,
			}
			changed = true
		}

		retargeted.SetProvider(target, lock.Version(), lock.VersionConstraints(), lock.AllHashes())
		onDisk[target] = addr
	}

	if !changed {
		return locks, nil
	}

	return retargeted, onDisk
}
