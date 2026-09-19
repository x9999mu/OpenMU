#!/bin/sh
set -eu

branch=${1:-develop}
expected_data_version=160
repo_root=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
lan_config_dir=/home/vietlubu/.config/openmu
compose_dir="$repo_root/deploy/all-in-one"
image_repository=${OPENMU_IMAGE_REPOSITORY:-ghcr.io/x9999mu/openmu}

cd "$repo_root"
previous_head=$(git rev-parse HEAD)

if [ -n "$(git status --porcelain)" ]; then
    echo "Refusing to deploy a dirty working tree." >&2
    exit 1
fi

for required_file in "$lan_config_dir/admin.env" "$lan_config_dir/docker-compose.lan.yml"; do
    if [ ! -f "$required_file" ]; then
        echo "Missing deployment configuration: $required_file" >&2
        exit 1
    fi
done

git fetch origin "$branch"
git checkout "$branch"
git pull --ff-only origin "$branch"
if [ "$previous_head" != "$(git rev-parse HEAD)" ]; then
    exec "$0" "$branch"
fi

revision=$(git rev-parse HEAD)
OPENMU_IMAGE="$image_repository:sha-$revision"
export OPENMU_IMAGE

compose() {
    sudo -n env OPENMU_IMAGE="$OPENMU_IMAGE" docker compose \
        -p openmu \
        --project-directory "$compose_dir" \
        --env-file "$lan_config_dir/admin.env" \
        -f "$compose_dir/docker-compose.yml" \
        -f "$lan_config_dir/docker-compose.lan.yml" \
        "$@"
}

sudo -n docker pull "$OPENMU_IMAGE"

compose stop openmu-startup
if ! compose run --rm --no-deps openmu-startup -applymandatoryupdates; then
    compose up -d --no-deps openmu-startup
    exit 1
fi

compose up -d --no-deps --force-recreate openmu-startup

elapsed=0
until sudo -n docker logs openmu-startup 2>&1 | grep -q "Host started"; do
    if [ "$elapsed" -ge 240 ]; then
        echo "OpenMU did not become ready within 240 seconds." >&2
        sudo -n docker logs --tail 100 openmu-startup >&2
        exit 1
    fi

    sleep 5
    elapsed=$((elapsed + 5))
done

installed_data_version=$(sudo -n docker exec database psql -U postgres -d openmu -Atc \
    "select \"CurrentInstalledVersion\" from config.\"ConfigurationUpdateState\" where \"InitializationKey\" = 'season6';")

if [ "${installed_data_version:-0}" -lt "$expected_data_version" ]; then
    echo "Database update failed: installed=$installed_data_version expected=$expected_data_version" >&2
    exit 2
fi

image_id=$(sudo -n docker inspect -f '{{.Image}}' openmu-startup)
echo "Deployed branch $branch at $(git rev-parse --short HEAD)"
echo "Container image: $OPENMU_IMAGE ($image_id)"
echo "Season 6 data version: $installed_data_version"
