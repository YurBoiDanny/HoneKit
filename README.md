# HoneKit

What the camera crew learns turns into tools and patches in a bag they carry.

## Run it

```bash
# the deployed shape: generate compose, build images, start the stack
aspire deploy -o aspire-output
# open http://localhost:8080

# inner loop: the services as local processes, with the Aspire dashboard
cd src/HoneKit.AppHost && dotnet run
```

`aspire-output/docker-compose.yaml` is generated from `src/HoneKit.AppHost`. Change the AppHost and
regenerate it; don't hand-edit the YAML. Images build for the host architecture, so an arm64 machine
gets arm64 images.

Seeded SQLite data lives in three named volumes (`content-data`, `progress-data`, `roster-data`).
Remove those volumes to reset the demo.
