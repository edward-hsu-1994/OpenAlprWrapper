CONFIGURATION ?= Release
SOLUTION := OpenAlprWrapper.sln
LIBRARY_PROJECT := src/OpenAlprWrapper/OpenAlprWrapper.csproj
PACKAGE_OUTPUT_DIR ?= artifacts/packages
NATIVE_BUILD_DIR ?= build/native

.PHONY: build test nuget pack clean native-build

build:
	dotnet build $(SOLUTION) --configuration $(CONFIGURATION)

test:
	dotnet test $(SOLUTION) --configuration $(CONFIGURATION)

nuget:
	dotnet pack $(LIBRARY_PROJECT) --configuration $(CONFIGURATION) --output $(PACKAGE_OUTPUT_DIR)

pack: nuget

clean:
	dotnet clean $(SOLUTION) --configuration $(CONFIGURATION)

native-build:
	cmake -S native/openalpr-dotnet-bridge -B $(NATIVE_BUILD_DIR)
	cmake --build $(NATIVE_BUILD_DIR) --config $(CONFIGURATION)
