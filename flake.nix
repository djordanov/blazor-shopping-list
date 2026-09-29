{
  description = "A simple web app for cashflow tracking";

  inputs = {
    nixpkg.url = "nixpkgs/nixos-26.05";
    aspire-cli.url = "github:microsoft/aspire";
  };

  outputs = { nixpkgs, aspire-cli, ... }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs
      {
        inherit system;
        config.allowUnfree = true;
      };
      # NOTE: everything below (LD_LIBRARY_PATH export + patchelf hook)
      # is a workaround for NixOS blocking generic dynamically-linked
      # binaries (Aspire's extracted dcp/aspire-managed). Enabling
      # programs.nix-ld.enable = true in the NixOS configuration would
      # deprecate ALL of it — the env var and the hook become harmless
      # leftovers and can be removed.
      aspireLibs = pkgs.lib.makeLibraryPath [
        pkgs.glibc
        pkgs.openssl
        pkgs.stdenv.cc.cc.lib   # libstdc++
        pkgs.icu
      ];
    in
    {
      devShells.${system}.default = pkgs.mkShell {
        packages = with pkgs; [
          csharp-ls
          postgresql
          dotnet-sdk_10
          jetbrains.rider
          patchelf
        ] ++ [
          aspire-cli.packages.${system}.aspire-cli
        ];
        shellHook = ''
          echo "Entered Flake"

          # DEPRECATED BY nix-ld: programs.nix-ld.enable = true provides a
          # working loader + library path for generic binaries, removing the
          # need for this export and the patchelf hook below.
          # Libraries for Aspire's extracted bundle binaries (generic ELF binaries).
          export LD_LIBRARY_PATH="${aspireLibs}''${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"

          # DEPRECATED BY nix-ld: see note above. Only needed because the
          # bundle binaries hardcode /lib64/ld-linux-x86-64.so.2, which is
          # NixOS's stub. With nix-ld enabled that path is a real loader
          # and no patching is required.
          # Patch the Aspire bundle loader if a bundle is extracted and unpatched.
          # Idempotent: skipped when already patched or no bundle exists yet.
          ASPIRE_BUNDLE_MANAGED=$(ls -dt "$HOME"/.aspire/versions/*/managed/aspire-managed 2>/dev/null | head -1)
          if [ -n "$ASPIRE_BUNDLE_MANAGED" ] && [ "$(${pkgs.patchelf}/bin/patchelf --print-interpreter "$ASPIRE_BUNDLE_MANAGED" 2>/dev/null)" = "/lib64/ld-linux-x86-64.so.2" ]; then
            for f in "$ASPIRE_BUNDLE_MANAGED" "''${ASPIRE_BUNDLE_MANAGED%managed/*}dcp/dcp"; do
              ${pkgs.patchelf}/bin/patchelf --set-interpreter "${pkgs.glibc}/lib/ld-linux-x86-64.so.2" "$f" 2>/dev/null || true
            done
            echo "Patched Aspire bundle interpreters for NixOS."
          fi
        '';
      };
    };
}
