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

# Run each test project separately so coverage output does not collide across
# multiple test host processes writing to the same path. Each project emits
# coverage to $(COVERAGE_DIR)/<ProjectName>/coverage.cobertura.xml.
coverage: build
	rm -rf $(COVERAGE_DIR)
	mkdir -p $(COVERAGE_DIR)
	@for proj in $(TEST_PROJECTS); do \
		name=`basename $$proj .csproj`; \
		echo "==> $$name"; \
		dotnet test --project $$proj \
			--configuration Release \
			--no-build \
			--coverage \
			--coverage-output-format cobertura \
			--coverage-output coverage.cobertura.xml \
			--results-directory $(COVERAGE_DIR)/$$name; \
	done

# Merge per-project cobertura files into a single HTML report and text summary.
coverage-report: coverage
	dotnet reportgenerator \
		-reports:$(COVERAGE_DIR)/**/coverage.cobertura.xml \
		-targetdir:$(COVERAGE_DIR)/html \
		-reporttypes:Html
	dotnet reportgenerator \
		-reports:$(COVERAGE_DIR)/**/coverage.cobertura.xml \
		-targetdir:$(COVERAGE_DIR) \
		-reporttypes:TextSummary
	@echo "Report: $(CURDIR)/$(COVERAGE_DIR)/html/index.html"
