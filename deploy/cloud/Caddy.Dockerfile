# The stock caddy image does not bundle the Cloudflare DNS plugin needed for the DNS-01 challenge.
# Versions are pinned for a reproducible rebuild.
FROM caddy:2.11.4-builder AS builder
RUN xcaddy build v2.11.4 --with github.com/caddy-dns/cloudflare@v0.2.4

FROM caddy:2.11.4
COPY --from=builder /usr/bin/caddy /usr/bin/caddy
