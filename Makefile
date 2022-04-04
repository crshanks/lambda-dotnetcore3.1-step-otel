STACK_NAME=sam-app-step-otel

.PHONY: build

build:
	sam build

deploy: build
	sam deploy \
		--capabilities CAPABILITY_NAMED_IAM \
		--parameter-overrides "newRelicLicenseKey=${NEW_RELIC_LICENSE_KEY}" "newRelicEndpoint=otlp.nr-data.net:4317" \
		--resolve-s3 \
		--stack-name "${STACK_NAME}" \
		--profile crsh-aws \
		--region eu-west-2

