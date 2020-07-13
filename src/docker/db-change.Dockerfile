FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine

RUN apk update && apk upgrade && \
    apk add --no-cache \
    git \
    openssh \
    icu-libs

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENV PATH="/db-change:${PATH}"

ADD . /db-change
WORKDIR /db-change

RUN chmod +x db-change
RUN chmod +x action-runner.sh

WORKDIR /