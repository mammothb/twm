COVERAGE_DIR := ./coverage

.PHONY: restore build test clean coverage coverage-report

restore:
	dotnet restore

build: restore
	dotnet build --configuration Release --no-restore

test: build
	dotnet test --configuration Release --no-build

clean:
	rm -rf $(COVERAGE_DIR)
	find . -type d \( -name bin -o -name obj \) -exec rm -rf {} + 2>/dev/null || true
	dotnet clean

# --- Coverage ---
TEST_PROJECTS := $(wildcard tests/*/*.Tests.csproj)

# coverlet.MTP writes per-test files as <prefix>.coverage.<timestamp>.cobertura.xml
# under --results-directory. --coverlet-file-prefix scopes each project to its
# own prefix. [ExcludeFromCodeCoverage] is excluded by coverlet's defaults.
coverage: build
	rm -rf $(COVERAGE_DIR)
	mkdir -p $(COVERAGE_DIR)
	@status=0; \
	for proj in $(TEST_PROJECTS); do \
		name=`basename $$proj .csproj`; \
		echo "==> $$name"; \
		dotnet test --project $$proj \
			--configuration Release \
			--no-build \
			--coverlet \
			--coverlet-output-format cobertura \
			--coverlet-file-prefix $$name \
			--results-directory $(COVERAGE_DIR)/$$name || status=$$?; \
	done; \
	exit $$status

# Merge per-project cobertura files into a single HTML report and text summary.
coverage-report: coverage
	dotnet reportgenerator \
		-reports:$(COVERAGE_DIR)/**/*.cobertura.xml \
		-targetdir:$(COVERAGE_DIR)/html \
		-reporttypes:Html
	dotnet reportgenerator \
		-reports:$(COVERAGE_DIR)/**/*.cobertura.xml \
		-targetdir:$(COVERAGE_DIR) \
		-reporttypes:TextSummary
	@echo "Report: $(CURDIR)/$(COVERAGE_DIR)/html/index.html"
