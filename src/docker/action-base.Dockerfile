FROM alpine
LABEL maintainer Terence Siganakis <terence@growingdata.com.au>
RUN apk --update add git less openssh docker curl && \
    rm -rf /var/lib/apt/lists/* && \
    rm /var/cache/apk/*
