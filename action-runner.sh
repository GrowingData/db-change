#!/bin/sh

DB_CONNECTION=$1
DB_ENVIRONMENT=$2
PATH_DBUP=$3
PATH_SCHEMA=$3
GIT_BRANCH=$3
GIT_EMAIL=$3
GIT_NAME=$3
GIT_NAME=$3


echo "Configuring git"
git --version
git config --local user.email "$GIT_EMAIL"
git config --local user.name "$GIT_NAME"

# Only allow fast forwards when merging
git config pull.ff only

echo "Checking out schema branch: ${GIT_BRANCH}"
git fetch
git checkout $GIT_BRANCH
git pull --unshallow

echo "Running db-change up..."
db-change up --path "$PATH_DBUP" --engine mssql --connection "$DB_CONNECTION"

echo "Generating full scripts for environment: ${DB_ENVIRONMENT}"
ds8-db script --path "$PATH_SCHEMA" --engine mssql --connection "$DB_CONNECTION"

export CURRENT_BRANCH="$(git symbolic-ref --short HEAD)"
echo "Pushing local changes from: ${CURRENT_BRANCH}"

git add . --all
if ! git diff-index --quiet HEAD --; then
    echo "Changes detected, commiting and pushing..."
    git commit -m "Database schema changes synchronized for $DB_NAME on $DB_SERVER (using environment: $DB_ENV)"
    git push origin master
else
    echo "No schema changes were detected"
fi
