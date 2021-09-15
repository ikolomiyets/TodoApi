# TodoApi
This is a sample API that illustrates the use of the RabbitMQ library in processing
API requests asynchronously.
There are four standard Rest API endpoints available:
* GET /api/todoitems - retrieving all todo items
* GET /api/todoitems/<id> - retrieving a todo item, identified by id
* POST /api/todoitems - creates a new todo item
* PUT /api/todoitems/<id> - updates a todo item, identified by id
* DELETE /api/todoitems/<id> - deletes a todo item, identified by id

Todo items are stored in the in-memory database.

The format of the todo item is:

Name | Type | Description
---|---|---
id | int | Todo item id
name | string | Todo item name
isComplete | boolean | Flag indicating if item is complete

For example:

```json
{
  "id": 3,
  "name": "walk dog",
  "isComplete": true
}
```

When sending a new object to the POST endpoint, id should be omitted, for example:
```json
{
  "name": "walk dog",
  "isComplete": true
}
```

Two of the requests are processed asynchronously: POST and DELETE.
The POST controller also illustrates technique how to get response from the asynchronous consumer.

The idea is to create a temporary queue, in this case we use the same prefix as the others and randomly generated UUID and bind the queue
to the same routing key.
When controller sends the message to the consumer for processing, it then tries to get the message from the 
temporary queue. If attempts fail after number of seconds controller just returns the original request.
If it succeeds in receiving the message back, it returns the todo item with the newly assigned id.
Then, temporary queue is deleted.

The temporary queue has the TTL assigned to it, identified by the rabbit__ttl environment variable (or rabbit.Ttl property). The ttl is the queue time to live in milliseconds.
If it is not deleted after TTL milliseconds, the queue will be deleted automatically.

## RabbitMQ configuration
There are number of the RabbitMQ configuration properties that could be set in `appsettings.json` or via environment variables:
All properties are set in the "rabbit" object.

Name | Environment Variable | Default Value | Description
---|---|---|---|
HostName | rabbit__hostname | localhot | The RabbitMQ host name
Port | rabbit__port | 5672 | The RabbitMQ port
VHost | rabbit__vhost | / | The virtual host
UserName | rabbit__username | N/A | The RabbitMQ username
Password | rabbit__password | N/A | The above user password
Ttl | rabbit__ttl | 60 | The temporary queue time-to-live in seconds
RetrieveTimeout | rabbit_retrievetimeout | 10 | Time in seconds which controller waits until giving up the attempts to get response from the consumer 
Prefix | rabbit_prefix | Todo | A prefix that will be added to all the echange, queue and routing keys names.

For example
```json
{
  "rabbit": {
      "UserName": "todo",
      "Password": "password",
      "Hostname": "localhost"
  }
}
```

### Prefix
Prefix is used to fine tune access for the application's RabbitMQ user. When user is created, they have to be granted all Configure, Write and Read access to the following
regex: <Prefix>.*

For example: `Todo.*`. Then, user will be able to create all necessary exchanges, queues and bindings.

If we would like to deploy another instance of the application that is using the same RabbitMQ instance, we just need
to change prefix and assign correct rights to the user that handles the second instance. In this case, both instances 
will use their own exchanges and own sets of queues.
