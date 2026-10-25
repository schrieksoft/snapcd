package main

// Builds the provider factories a plan needs from the providers `init` already
// cached in the module directory.
//
// OpenTofu does this in Meta.providerFactories and providerFactory, both
// unexported, so the wiring is reproduced here. The handshake, plugin set and
// protocol negotiation all come from OpenTofu's own exported values, so nothing
// about the plugin protocol is restated: what is reproduced is only the lookup
// of a cached provider and the client setup around it.
//
// The dev-override and unmanaged-provider branches are deliberately absent. Both
// exist for provider development, and a probe plans a module a job has already
// inited.

import (
	"errors"
	"fmt"
	"os"
	"os/exec"

	"github.com/hashicorp/go-plugin"

	"github.com/opentofu/opentofu/internal/addrs"
	"github.com/opentofu/opentofu/internal/depsfile"
	"github.com/opentofu/opentofu/internal/logging"
	tfplugin "github.com/opentofu/opentofu/internal/plugin"
	tfplugin6 "github.com/opentofu/opentofu/internal/plugin6"
	"github.com/opentofu/opentofu/internal/providercache"
	"github.com/opentofu/opentofu/internal/providers"
)

// Matches OpenTofu's own switch for the same variable.
var enableProviderAutoMTLS = os.Getenv("TF_DISABLE_PLUGIN_TLS") == ""

// providerFactories returns a factory per provider the lock file selects, taken
// from the local cache that `init` populated.
func providerFactories(
	locks *depsfile.Locks,
	cacheDirPath string,
	onDisk providerAddrMap,
) (map[addrs.Provider]providers.Factory, error) {
	cacheDir := providercache.NewDir(cacheDirPath)
	factories := map[addrs.Provider]providers.Factory{}

	for provider, lock := range locks.AllProviders() {
		version := lock.Version()

		// The package sits under the address whichever tool installed it wrote,
		// which is not always the address the engine asks for.
		pkgAddr := provider
		if mapped, ok := onDisk[provider]; ok {
			pkgAddr = mapped
		}

		cached := cacheDir.ProviderVersion(pkgAddr, version)
		if cached == nil {
			return nil, fmt.Errorf(
				"there is no package for %s %s cached in %s; run init",
				pkgAddr, version, cacheDir.BasePath())
		}

		// The checksum check is what makes this the same provider the rest of the
		// job runs, rather than whatever happens to sit in the directory.
		ok, err := cached.MatchesAnyHash(lock.PreferredHashes())
		if err != nil {
			return nil, fmt.Errorf(
				"failed to verify the package for %s %s in %s: %w",
				provider, version, cacheDir.BasePath(), err)
		}
		if !ok {
			return nil, fmt.Errorf(
				"the cached package for %s %s (in %s) does not match any of the checksums recorded in the dependency lock file; run tofu init",
				provider, version, cacheDir.BasePath())
		}

		factories[provider] = providerFactory(cached)
	}

	return factories, nil
}

func providerFactory(meta *providercache.CachedProvider) providers.Factory {
	schemaCache := providers.NewSchemaCache()

	return func() (providers.Interface, error) {
		execFile, err := meta.ExecutableFile()
		if err != nil {
			return nil, err
		}

		config := &plugin.ClientConfig{
			HandshakeConfig:  tfplugin.Handshake,
			Logger:           logging.NewProviderLogger(""),
			AllowedProtocols: []plugin.Protocol{plugin.ProtocolGRPC},
			Managed:          true,
			Cmd:              exec.Command(execFile),
			AutoMTLS:         enableProviderAutoMTLS,
			VersionedPlugins: tfplugin.VersionedPlugins,
			SyncStdout:       logging.PluginOutputMonitor(fmt.Sprintf("%s:stdout", meta.Provider)),
			SyncStderr:       logging.PluginOutputMonitor(fmt.Sprintf("%s:stderr", meta.Provider)),
		}

		client := plugin.NewClient(config)
		rpcClient, err := client.Client()
		if err != nil {
			return nil, err
		}

		raw, err := rpcClient.Dispense(tfplugin.ProviderPluginName)
		if err != nil {
			return nil, err
		}

		switch protoVer := client.NegotiatedVersion(); protoVer {
		case 5:
			p := raw.(*tfplugin.GRPCProvider)
			p.PluginClient = client
			p.SchemaCache = schemaCache
			return p, nil
		case 6:
			p := raw.(*tfplugin6.GRPCProvider)
			p.PluginClient = client
			p.SchemaCache = schemaCache
			return p, nil
		default:
			return nil, errors.New("the provider negotiated an unsupported protocol version")
		}
	}
}
