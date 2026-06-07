# MetaTube DMM Image Mapping Design

## Context

MetaTube returns JavBus poster and thumbnail URLs for JavBus movies. MetaJavarr now uses the same SOCKS5 proxy configuration as Radarr, so the live pod can reach JavBus through the proxy. Direct pod access still times out.

Through the proxy, plain MetaJavarr media-cover requests to JavBus image URLs receive `403 Forbidden`. A browser-like request with a user agent and JavBus referer can fetch the same image, which shows the issue is JavBus hotlink/client filtering rather than a region-only block.

MetaTube movie detail responses also include DMM `preview_images`. For sampled titles, DMM package image URLs derived from the preview path have the same dimensions as the JavBus covers:

| Movie | JavBus Cover | DMM Cover |
| --- | --- | --- |
| IDBD-894 | 800x538, 234 KB | 800x538, 208 KB |
| IPX-159 | 800x538, 162 KB | 800x538, 158 KB |

## Decision

Use DMM-derived image URLs for JavBus movies when MetaTube provides DMM preview images.

For a DMM preview image like:

```text
https://pics.dmm.co.jp/digital/video/ipx00159/ipx00159jp-1.jpg
```

MetaJavarr maps:

- Poster to `https://pics.dmm.co.jp/digital/video/ipx00159/ipx00159pl.jpg`
- Fanart to the first DMM preview image

This is a hard cutover for JavBus movies with DMM preview metadata. MetaJavarr will not try to maintain a compatibility path that depends on browser-like headers for JavBus image downloads.

## Tradeoff

JavBus covers are slightly larger in sampled files, but DMM covers have the same dimensions and are reachable without spoofing a browser session. Browser-like headers are brittle because they depend on Cloudflare/JavBus accepting hotlink-style requests without a real age-verification browser flow. DMM image URLs are simpler, stable enough for automated media-cover downloads, and already present in MetaTube metadata.

FC2Hub image handling remains unchanged. If MetaTube does not provide a usable DMM preview image, MetaJavarr keeps the original image URL.

## Verification

- Add regression coverage for JavBus image mapping from DMM preview images.
- Preserve the existing FC2Hub `javfree` mapping behavior.
- Verify focused MetaTube proxy tests in Cloud Shell Docker.
- Deploy a new immutable MetaJavarr image tag and confirm live movie metadata uses DMM image URLs.
