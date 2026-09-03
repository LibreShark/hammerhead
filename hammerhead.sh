cd -- "$(dirname -- "${BASH_SOURCE[0]}")" || exit 1

dotnet run --project dotnet/src/src.csproj --framework net9.0 -- "$@"
