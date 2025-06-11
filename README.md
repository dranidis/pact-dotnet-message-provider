# Pact message provider tests in C#

An example using `description` and `provider-states`

Execute tests with:

```
dotnet test
```

A file `pact.log` is created showing the sequence of execution during two interactions sharing the same `description` but different `providerStates` (see the pact file `MessageConsumer-MessageProvider.json`):

```
Description handler: user was created event
Creating metadata for user creation event
State handler - user registers with email and password
WithContent: user was created event
Creating message for user creation event, email and password registered
State handler - user registers with social account
WithContent: user was created event
Creating message for user creation event, social account registered
```
