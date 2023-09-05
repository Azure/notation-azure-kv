# Notation Azure KeyVault Plugin Makefile for Linux
BUILD_DIR = ./bin
PROJECT_DIR = ./Notation.Plugin.AzureKeyVault
TEST_PROJECT_DIR = ./Notation.Plugin.AzureKeyVault.Tests

.PHONY: help
help:
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-25s\033[0m %s\n", $$1, $$2}'

.PHONY: all
all: build

.PHONY: FORCE
FORCE:

.PHONY: build
build: ## builds binaries
	dotnet build $(PROJECT_DIR) -c Release -o $(BUILD_DIR) --self-contained true /p:PublishSingleFile=true

.PHONY: test
test: ## run unit test
	dotnet test $(TEST_PROJECT_DIR) --collect:"XPlat Code Coverage" --logger trx --results-directory $(BUILD_DIR)/TestResults

.PHONY: install
install: bin/notation-azure-kv ## installs the plugin
	mkdir -p  ~/.config/notation/plugins/azure-kv/
	cp bin/notation-azure-kv ~/.config/notation/plugins/azure-kv/