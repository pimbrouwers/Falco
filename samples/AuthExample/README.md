# AuthExample

This sample provide an example on how to configure Falco with endpoints protected by authorization.

The service is configured with four endpoints that requires different scopes.

A scope can be seen as a group of claims provided from an authorization server.
For this example, four different scopes are defined:
- **read:users** (grants the capability to read the users)
- **create:users** (grants the capability to create users)
- **update:users** (grants the capability to update users)
- **delete:users** (grants the capability to delete users)

The scope request parameter is a space separated, case-sensitive list of scopes. So it may contain several scopes.

To obtain the access token:
```shell
curl --location --request POST 'https://falco-auth-test.us.auth0.com/oauth/token' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'grant_type=password' \
--data-urlencode 'client_id=ezsAcSNwmF6f4EWH5pfMdvh9p2HSlPpu' \
--data-urlencode 'audience=https://users/api' \
--data-urlencode 'username=test@test.com' \
--data-urlencode 'password=Test123#@' \
--data-urlencode 'scope=create:users update:users read:users'
```
*NOTE: this is an environment set for test purpose, don't use it in production!*

To get the users:
```shell
curl --location --request GET 'http://localhost:5000/users' \
--header 'Authorization: Bearer <ACCESS_TOKEN>'
```

To create an user:
```shell
curl --location --request POST 'http://localhost:5000/users' \
--header 'Authorization: Bearer <ACCESS_TOKEN>' \
--header 'Content-Type: application/json' \
--data-raw '{
    "Username": "user4",
    "Name": "Son",
    "Surname": "Jun"
}'
```

To update an user:
```shell
curl --location --request PUT 'http://localhost:5000/users/<USER_ID>' \
--header 'Authorization: Bearer <ACCESS_TOKEN>' \
--header 'Content-Type: application/json' \
--data-raw '{
    "Username": "user4-update",
    "Name": "Son",
    "Surname": "Niohn"
}'
```

To delete an user:
```shell
curl --location --request DELETE 'http://localhost:5000/users/<USER_ID>' \
--header 'Authorization: Bearer <ACCESS_TOKEN>'
```