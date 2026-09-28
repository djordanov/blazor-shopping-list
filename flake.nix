{
  description = "A simple web app for cashflow tracking";

  inputs.nixpkg.url = "nixpkgs/nixos-26.05";

  outputs = { nixpkgs, ... }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs
      {
        system = "${system}";
        config.allowUnfree = true;
      };
    in
    {
      devShells.${system}.default = pkgs.mkShell {
        packages = with pkgs; [ csharp-ls postgresql dotnet-sdk_10 jetbrains.rider ];
        shellHook = ''
          echo "Entered Flake"
        '';
        };
    };
}
