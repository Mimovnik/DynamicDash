{
  description = "dotnet development environment";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    flake-parts.url = "github:hercules-ci/flake-parts";
  };

  outputs = inputs @ {
    self,
    nixpkgs,
    flake-parts,
    ...
  }:
    flake-parts.lib.mkFlake {inherit inputs;} {
      systems = ["x86_64-linux" "aarch64-linux" "aarch64-darwin" "x86_64-darwin"];
      perSystem = {
        config,
        system,
        ...
      }: let
        pkgs = import nixpkgs {
          inherit system;
          config.allowUnfree = true;
        };
      in {
        devShells.default = pkgs.mkShell {
          buildInputs =
            with pkgs;
              [
                dotnet-sdk_9
                dotnet-runtime_9

                git
                curl
                unzip
                jq

                fontconfig
                freetype
                libglvnd
                xorg.libX11
                xorg.libxcb
                xorg.libXcursor
                xorg.libXrender
                xorg.libXrandr
                xorg.libXi
                xorg.libXtst
                xorg.libXinerama
                xorg.libXfixes
                xorg.libSM
                xorg.libICE
                harfbuzz
                pango
                cairo
                openssl
                zlib
              ];

          shellHook = ''
            # Configure .NET environment
            export DOTNET_ROOT="${pkgs.dotnet-sdk_9}"
            export DOTNET_CLI_TELEMETRY_OPTOUT=1
            export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
            export DOTNET_NOLOGO=1
            
            # Add .NET tools to PATH
            export PATH="$HOME/.dotnet/tools:$PATH"
            
            # Set up NuGet config to use system packages when possible
            export NUGET_PACKAGES="$HOME/.nuget/packages"

            # Make native libraries required by Avalonia/Skia visible at runtime
            export LD_LIBRARY_PATH="${pkgs.lib.makeLibraryPath [
              pkgs.fontconfig
              pkgs.freetype
              pkgs.libglvnd
              pkgs.xorg.libX11
              pkgs.xorg.libxcb
              pkgs.xorg.libXcursor
              pkgs.xorg.libXrender
              pkgs.xorg.libXrandr
              pkgs.xorg.libXi
              pkgs.xorg.libXtst
              pkgs.xorg.libXinerama
              pkgs.xorg.libXfixes
              pkgs.xorg.libSM
              pkgs.xorg.libICE
              pkgs.harfbuzz
              pkgs.pango
              pkgs.cairo
              pkgs.openssl
              pkgs.zlib
            ]}:$LD_LIBRARY_PATH"
            
            echo ".NET SDK version:"
            dotnet --version
            echo ""
            echo "PATH includes .NET tools: $HOME/.dotnet/tools"
            echo ""
          '';
        };
      };
    };
}
