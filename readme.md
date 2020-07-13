# DbChange

DbChange is a simple tool for managing database schema changes. Leveraging DbUp, DbChange provides a simple way to run database upgrade scripts to do things like:

- Create tables
- Add columns
- Create indexes
- Insert default data

DbChange also exports the schema of your database after it applies each change, ensuring that you have a record of each change within source control. This makes it simple to use git to see how objects have changed over time.

## Supported databases

At the moment we only support Microsoft SQL Server, but plan support for PostgreSQL soon.

## Parameters:

### db-connection

The ADO.Net connection string, which looks something like:
`Server=localhost;Initial Catalog=<DATABASE_NAME>;User ID=<USERNAME>;Password=<PASSWORD>;Connection Timeout=5;`

### db-environment

The name of the environment you are managing with this action. Typically this will be something like "dev", "prod", "staging"

### path-dbup-scripts

The path to a directory containing .sql scripts to be used to upgrade the schema of the database. Scripts here should be named such that they are always well ordered, as there sort order will determine their execution order. A good pattern to use here is to name your files `{yyyy-MM-dd}-{sequence-numeber}-{obj-name}.sql`, where an example might be: `2020-04-11-003-product_category.sql`.

### path-schema-scripts

The path that DbChange will use to write your latest set of schema files (with one file per object).

### git-schema-branch

The branch to use to write changes to the database schema.

### git-email

The email address of the git identity used to apply the changes

### git-name

The name of the git identity used to apply the changes.

## Usage in GitHub Actions

You can use DbChange to manage a database schema within CI/CD using the following GitHub Action.

Create as new workflow file:

In your repository, create a workflow file:
./github/workflows/db-schema-dev.yml

```
name: db-schema-dev
# Run on all pushes
on: push

jobs:
  apply-schema-changes:
    # Use a self-hosted runner running within your network perimeter so that
    # you can connect directly to the database server
    runs-on: [self-hosted, linux]

    # We need to specify the container to ensure that we have a container with
    # git and docker installed.
    container:
      image: growingdata/github-action-base:v1.0.3
    steps:
      # Check out the repository
      - name: checkout-code
        uses: actions/checkout@v2

      # Apply the changes
      - name: apply-db-changes
        uses: GrowingData/db-change@v1.0.3
        with:
          db-connection: ${{ secrets.DB_CONNECT_STRING }}
          db-environment: "dev"
          path-dbup-scripts: "./db-change/upgrades"
          path-schema-scripts: "./db-change/schemas"
          git-schema-branch: "master"
          git-email: "schema-sync@growingdata.com.au"
          git-name: "Schema Robot"
```
