
# Email Contact Services

Is a ServiceStack Single Page App built just using jQuery and Bootstrap to provide an example showing how to structure a medium-sized ServiceStack solution as well as showcasing some of ServiceStack's built-in features useful in the reducing the effort for developing medium-sized Web Applications.

The purpose of the EmailContacts Application is to manage contacts in any RDBMS database, provide a form to be able to send them messages and maintain a rolling history of any emails sent. The application also provides an option to have emails instead sent and processed via [Rabbit MQ](https://github.com/ServiceStack/ServiceStack/wiki/Rabbit-MQ).

![EmailContacts Screenshot](https://raw.github.com/ServiceStack/EmailContacts/master/src/EmailContacts/Content/splash.png)

The entire UI is maintained in a single 
[default.cshtml](https://github.com/ServiceStack/EmailContacts/blob/master/src/EmailContacts/default.cshtml) 
page under 130 lines of HTML and 70 lines of JavaScript to render the dynamic UI, bind server validation errors and provide real-time UX feedback. 
The Application also follows an API-First development style where the Ajax UI calls only published APIs allowing 
all services to be immediately available, naturally, via an end-to-end typed API to Mobile and Desktop .NET clients.

This documentation goes through setting up this solution from scratch, and explains the ServiceStack features it makes use of along the way.

## Table of Contents

  - [Creating EmailContacts Solution from Scratch](https://github.com/ServiceStack/EmailContacts#creating-emailcontacts-solution-from-scratch)
    - Add ServiceStack II7+ handler mapping
    - Install NuGet Packages
  - [The AppHost](https://github.com/ServiceStack/EmailContacts#the-apphost)
    - Plugins
    - OrmLite
    - Accessing AppSettings
    - Registering Dependencies
    - Profiling
      - SQL Profiling
  - [HTML Features](https://github.com/ServiceStack/EmailContacts#html-features)
    - Razor Pages
    - Accessing data in views
      - Accessing Db Directly
      - Accessing Services and Dependencies
      - Embedded JSON
      - Loaded via Ajax
      - View Model
  - [API-first development](https://github.com/ServiceStack/EmailContacts#api-first-development)
    - ServiceStack JavaScript Utils - /js/ss-utils.js
    - Bootstrap Forms
      - Binding HTML Forms
      - Fluent Validation
    - Declarative Events
    - Data Binding
    - Advanced bindForm usages
      - Form Loading
      - Server initiated actions
  - [Message queues](https://github.com/ServiceStack/EmailContacts#message-queues)
    - Benefits of Message Queues
    - Using an MQ for sending Emails
    - Rabbit MQ
    - Configuring an MQ Server in ServiceStack
    - Reliable and Durable Messaging
    - Deferred Execution and Instant Response Times
  - [Integration Tests](https://github.com/ServiceStack/EmailContacts#integration-tests)
  - [Unit Tests](https://github.com/ServiceStack/EmailContacts#unit-tests)
  - [Further Reading](https://github.com/ServiceStack/EmailContacts#further-reading)

## Creating EmailContacts Solution from Scratch

This section will take you through the steps for physically laying out setting up a typical ServiceStack Razor + Web Services solution from scratch:

  - Create new **EmailContacts** Empty ASP.NET Web Application
  - Add New Class Library Projects to solution:
    - **EmailContacts.ServiceInterface** - For Service implementations
    - **EmailContacts.ServiceModel** - For impl/dependency-free DTOs
    - **EmailContacts.Tests** - For NUnit Unit and Integration Tests

### Add ServiceStack II7+ handler mapping

Adding this to the [Web.config](https://github.com/ServiceStack/EmailContacts/blob/master/src/EmailContacts/Web.config#L16-L21) tells ASP.NET to route all HTTP Requests to ServiceStack:

```xml
<system.webServer>
  <validation validateIntegratedModeConfiguration="false" />
  <handlers>
    <add path="*" name="ServiceStack.Factory" 
          type="ServiceStack.HttpHandlerFactory, ServiceStack" 
          verb="*" preCondition="integratedMode" resourceType="Unspecified" allowPathInfo="true" />
  </handlers>
</system.webServer>
```

### Install NuGet Packages

For **EmailContacts** Host Project:

    PM> Install-Package jQuery
    PM> Install-Package bootstrap
    PM> Install-Package ServiceStack.Razor
    PM> Install-Package ServiceStack.Swagger
    PM> Install-Package ServiceStack.RabbitMq
    PM> Install-Package ServiceStack.OrmLite.Sqlite.Mono

For **EmailContacts.ServiceInterface** project:

    PM> Install-Package ServiceStack
    PM> Install-Package ServiceStack.OrmLite

For **EmailContacts.ServiceModel** DTO project:

    PM> Install-Package ServiceStack.Interfaces

Configure your model namespaces to be included in all Razor Pages by default by adding them to Web.config's Razor namespaces:

```xml
<pages pageBaseType="ServiceStack.Razor.ViewPage">
    <namespaces>
        ...
        <add namespace="EmailContacts.ServiceModel" />
        <add namespace="EmailContacts.ServiceModel.Types" />
    </namespaces>
</pages>
```

For **EmailContacts.Test** project:

    PM> Install-Package NUnit
    PM> Install-Package ServiceStack.RabbitMq
    PM> Install-Package ServiceStack.OrmLite.Sqlite.Mono

and set **Build > Platform Target** to **x86** so it can run the 32bit sqlite3.dll.

The next step is to setup your ServiceStack AppHost:

## The AppHost

The role of the AppHost is to be the central place where all your applications configuration should be defined, where all the plugins, filters and everything else your service needs should be configured. It also serves as the conduit for binding all your services concrete dependencies against their registered abstractions. Ideally the implementation of your service would then only depend on these substitutable interfaces making it possible to mock in testing.

There's only 1 AppHost in each ServiceStack solution which should be in your host project, by convention we put the AppHost in the Global.asax file, as it shares similar purposes where initialization code for your application that only runs once on StartUp is kept.

Below is the typical structure for every ServiceStack solution:

```csharp
public class AppHost : AppHostBase
{
    public AppHost() : base("Email Contact Services", typeof(ContactsServices).Assembly) {}

    public override void Configure(Container container)
    {
        SetConfig(new HostConfig { ... });
        ...
    }
}

public class Global : System.Web.HttpApplication
{
    protected void Application_Start(object sender, EventArgs e)
    {
        new AppHost().Init();
    }
}
```

  1. By convention the AppHost is called **AppHost** which for ASP.NET hosts inherits from `AppHostBase`
  1. The base constructor should be passed:
    - The name of your solution which appears as the title in the metadata pages
    - The assemblies ServiceStack should look for to register and autowire all your ServiceStack services
    - The `Configure()` method is where to place all your application configuration.
  1. Most of ServiceStack features are available as options in the `SetConfig()` method
    - Whilst all custom hooks in ServiceStack (e.g. filters and handlers) are exposed as properties in the base class
    - The configuration of your service should be immutable after the Configure is run on StartUp
  1. Initializing your AppHost is done by running `new AppHost().Init()` in the Global.asax **Application_Start** event

### Plugins

<img src="http://i.imgur.com/2Hf3P9L.png" width="350" align="right" hspace="10" />

Most of ServiceStack's high-level features are encapsulated in modular plugins that can be easily added and removed. Typically ServiceStack will register most of built-in plugins by default which can be easily, added, removed or configured. E.g. you can remove the Metadata pages with:

```csharp
Plugins.RemoveAll(x => x is MetadataFeature);
```

The [documentation on Plugins](https://github.com/ServiceStack/ServiceStack/wiki/Plugins) 
lists all the plugins that are available in ServiceStack and which ones are added by default. Other than the Plugins collection, you can view all the plugins loaded with your application by view the request info of any page with the query string [?debug=requestinfo](?debug=requestinfo) (which is itself a plugin :).

Most plugins have a **Feature** suffix to indicate it's providing some functionality and features to ServiceStack. The exception to this are Format's which provide some representation of your services. These include the built-in 
`CsvFormat`, `MarkdownFormat`, `HtmlFormat`. Whilst other Formats are available in external NuGet packages include: `MsgPackFormat`, `ProtoBufFormat` and `RazorFormat`.

#### Plugins Links

To make it easier to discover Plugins that have a Web UI are listed at the bottom of the metadata pages. The plugins that are publicly available are listed under **Plugin Links:** whilst plugins only available during **DebugMode** or to administrators are listed under the **Debug Links:** heading.

#### Description of plugins used

The new plugins available in this app are listed at the top of the Configure() method:

```csharp
Plugins.Add(new SwaggerFeature());
Plugins.Add(new RazorFormat());
Plugins.Add(new RequestLogsFeature());

Plugins.Add(new ValidationFeature());
container.RegisterValidators(typeof(ContactsServices).Assembly);
```

  - The `SwagggerFeature` provides a Swagger UI and supporting services visible at [/swagger-ui/](/swagger-ui/)
  - The `RazorFormat` contains ServiceStack's Razor support, described in detail on [razor.servicestack.net](http://razor.servicestack.net/)
  - The `RequestLogsFeature` allows you to view and query all requests processed by ServiceStack at [/requestlogs](/requestlogs)
  - The `ValidationFeature` adds [Fluent Validation](http://fluentvalidation.codeplex.com/documentation) support to ServiceStack
    - Use `container.RegisterValidators()` to ServiceStack in which Assemblies it can find all the validators it should automatically wire-up

### OrmLite

OrmLite is a fast, simple, convention-based, config-free lightweight ORM that uses code-first POCO classes to generate RDBMS table schemas. Its API's are simply extension methods over ADO.NET's underlying **System.Data** core interfaces providing DRY, easy-to-use, flexible and expressive APIs for common data access patterns that also includes a typed expression-based LINQ-like API for typed Data Access.

OrmLite has [providers for most major RDBMS's](https://github.com/ServiceStack/ServiceStack.OrmLite/#download) which are configured in the same way by registering the connection string and which dialect to use.

Sqlite is used in this demo since it's a file-based database that's self-contained and doesn't require any external dependencies (perfect for demos :). The Sqlite provider accepts file names for the connection string as well as the special **:memory:** string which tells Sqlite to use an in-memory database.

```csharp
container.Register<IDbConnectionFactory>(
    c => new OrmLiteConnectionFactory("db.sqlite", SqliteDialect.Provider));
```

Once registered we can make immediate use of OrmLite by resolving the DB Factory from the IOC and opening a data connection from it. OrmLite also supports the creation of tables based on the schema of code-first POCO's, which we use here to Drop and re-create both Email and Contact tables. After the tables are created we can initialize the app with some test data:

```csharp
using (IDbConnection db = container.Resolve<IDbConnectionFactory>().Open())
{
    db.DropAndCreateTable<Email>();
    db.DropAndCreateTable<Contact>();

    db.Insert(new Contact { Name = "Kurt Cobain", Email = "demo+kurt@servicestack.net", Age=27 });
    db.Insert(new Contact { Name = "Jimi Hendrix", Email = "demo+jimi@servicestack.net", Age=27 });
    db.Insert(new Contact { Name = "Michael J.", Email = "demo+mike@servicestack.net", Age=50 });
}
```

### Accessing AppSettings

Often you'll want to access application settings and provide them to your dependencies. ServiceStack provides a convenient AppSettings class that simplifies reading from Web.config appSettings and providing your own default complex type configuration if one does not exist:

```csharp
var appSettings = new AppSettings();

container.Register(appSettings.Get("SmtpConfig",
    new SmtpConfig {
        Host = "smtphost",
        Port = 587,
        UserName = "ADD_USERNAME",
        Password = "ADD_PASSWORD"
    }));
```

The config above will look for an [appSetting named SmtpConfig](https://github.com/ServiceStack/EmailContacts/blob/master/src/EmailContacts/Web.config#L26), 
if it exists it will deserialize it into a `SmtpConfig` class using the [JSV Format](https://github.com/ServiceStack/ServiceStack.Text/wiki/JSV-Format), otherwise it will use the default inline SmtpConfig provided. 
The registration above also registers an instance of SmtpConfig in the IOC so that will be injected into any dependency or Service that has a SmtpConfig Type **public property** or constructor argument.

Instead of using complex nested XML and configuration classes for maintaining structured configuration, the `AppSettings` class and JSV Format lets you dehydrate complex type configuration into clean POCO's with a single in-line appSetting value:

```xml
<appSettings>
  <add key="SmtpConfig" value="{Host:smtphost,Port:587,Username:ADD_USER,Password:ADD_PASS}" />
</appSettigns>
```

### Registering Dependencies

A common task for any non-trivial application is to register your own dependencies used by your services. We've already seen an example of how to register a dependency when we registered OrmLite, but that took a lambda which meant it took control over the objects construction.

[ServiceStack's built-in IOC](https://github.com/ServiceStack/ServiceStack/wiki/The-IoC-container) 
supports a number of other API's to register your dependencies, a common one is to have the IOC also auto wire any dependencies your dependencies might have. The same API allows you to register your concrete dependency against a different interface, i.e:

```csharp
container.RegisterAs<SmtpEmailer, IEmailer>().ReusedWithin(ReuseScope.Request);
//container.RegisterAs<DbEmailer, IEmailer>().ReusedWithin(ReuseScope.Request);
```

This application has 2 substitutable implementations of `IEmailer` available. If you have access to an SMTP server it can send emails using `SmtpEmailer`, otherwise register `DbEmailer` instead to simulate sending emails with a 1 second delay and track a history of them in a database.

### Profiling

ServiceStack also comes with an integrated version of [Mini Profiler](https://github.com/ServiceStack/ServiceStack/wiki/Built-in-profiling) which you enable by starting and stopping it in ASP.NET's global.asax Request events, e.g:

```csharp
protected void Application_BeginRequest(object src, EventArgs e)
{
    if (Request.IsLocal)
        Profiler.Start();
}

protected void Application_EndRequest(object src, EventArgs e)
{
    Profiler.Stop();
}
```

Once enabled the Mini Profiler UI will appear at the top-right of ServiceStack's auto HTML pages, e.g:

![Mini Profiler Basic](http://mono.servicestack.net/files/miniprofiler-hello.png)

#### SQL Profiling

The mini profiler also supports profiling SQL performed in services. To enable it you will need let Mini Profiler profile the connection which you can be done at registration:

```csharp
container.Register<IDbConnectionFactory>(
    c => new OrmLiteConnectionFactory("db.sqlite", SqliteDialect.Provider) {
        ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
    });
```

Once the Database connection is profiled it will start appearing in the Mini Profiler results view

![Simple DB Example](http://mono.servicestack.net/files/miniprofiler-simpledb.png)

The mini profiler also warns when it detects N+1 sql queries:

![Simple N+1 DB Example](http://mono.servicestack.net/files/miniprofiler-simpledb-n1.png)

Clicking on the link will open a new dialog to view the SQL queries that were performed for that request:

![SQL Viewer](http://mono.servicestack.net/files/miniprofiler-simpledb-n1-sql.png)

## HTML Features

In addition to providing a solid Services Framework, ServiceStack also serves as a full-featured Web Framework great for powering dynamic websites and javascript-powered Single Page Apps. With no other dependencies, it can create content-heavy and simple dynamic sites like the [ServiceStack Docs](http://mono.servicestack.net/docs/) website using just the built-in [Markdown Razor view engine](http://mono.servicestack.net/docs/markdown/markdown-razor) and static file handling support in ServiceStack.

### Razor Pages

To unlock the full-potential of the web framework it's highly recommended to enable Razor support which adds a number of its own features to simplify web development documented at [razor.servicestack.net](http://razor.servicestack.net).

ServiceStack's Razor support is contained in the **ServiceStack.Razor** NuGet package added at the start, to enable it just register the `RazorFormat` plugin in your AppHost:

```csharp
public override void Configure(Container container)
{
    Plugins.Add(new RazorFormat());
    ...
}
```

Now you're ready to start creating Razor pages which are just plain-text HTML pages with a **.cshtml** extension.

### The No Ceremony option - Dynamic pages without Controllers

A productive option all web frameworks should have is being able to execute dynamic pages directly without the boilerplate of an intermediate controller. ServiceStack has particularly good support for this story with a number of useful features:

  - **Pretty Urls by default** - Hitting F5 on a **page.cshtml** will auto redirect to the ideal `/page` route
  - **Default pages for directories** - Directories will execute its **default.cshtml** page and retain its pretty url
  - **Cascading Layout Templates** - Razor pages will automatically use the **_Layout.cshtml** that's nearest to the directory where the page is located
  - **Smart View Pages** - ServiceStack's Razor pages aren't crippled, they have full access to framework features, HTTP Request, deps, Services, etc
  - **Request ViewModel** - When executed directly, the Views **Model** is a dynamic object that looks at HTTP Headers, QueryString, FormData, etc

### Accessing data in views

As Razor Pages provide full access to framework features, it enables a few different ways to accesss data from within your pages, shown in [info.cshtml](https://github.com/ServiceStack/EmailContacts/blob/master/src/EmailContacts/info.cshtml):

#### Accessing Db Directly

If you register a DB Factory in your IOC you can use ADO.NET's `base.Db` IDbConnection property available in Pages and Services and take advantage of the convenience extension methods offered by Micro ORMs like 
[OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite/) and [Dapper](https://code.google.com/p/dapper-dot-net/). 
E.g you can view all the Contacts inserted in the AppHost using OrmLite's typed APIs with:

```html
<ul>
    @foreach (var contact in Db.Select<Contact>())
    {
        <li>@contact.Name @contact.Email (@contact.Age)</li>
    }
</ul>
```

#### Accessing Services and Dependencies

Rather than querying the DB directly another option is to query Services or Repositories which you can resolve from the IOC using `Get<T>`, e.g:

```html
<ul>
    @using (var service = Get<ContactsServices>())
    {
        var contacts = service.Any(new FindContacts());
        foreach (var contact in contacts)
        {
            <li>@contact.Name @contact.Email (@contact.Age)</li>
        }
    }
</ul>
```

This works because Services are themselves just registered dependencies that you can resolve from the IOC and execute as-is. The one caveat is if your services makes use of the HTTP Request object it will need to be either injected manually or instead of `Get<T>`, call `ResolveService<T>` which does it.

#### Embedded JSON

Often using JavaScript ends up being an easier and more flexible alternative to generating HTML than C#. One way to do this is to serialize C# models into JSON which as it's also valid JavaScript, can be accessed directly as a native JS Object. In ServiceStack this is as easy as using the `T.AsRawJson()` extension method:

```html
<ul id="embedded-json"></ul>

<script>
$("#embedded-json").append(
    contactsHtml(@(Db.Select<Contact>().AsRawJson())));

function contactsHtml(contacts) {
    return contacts.map(function (c) {
        return "<li>" + c.Name + " " + " (" + c.Age + ")" + "</li>";
    }).join('');
}
</script>
```

In this example `AsRawJson()` converts the C# collection into a JSON Array which is automatically inferred as a native JavaScript array when loaded by the browser. It's then passed to the `contactsHtml(contacts)` JavaScript function that converts it into a HTML string that's injected into the **#embedded-json** UL HTML element using jQuery's `$.append()`.

#### Loaded via Ajax

The popular alternative to using JavaScript to generate HTML is to load the JSON via Ajax, which as ServiceStack returns pure DTOs serialized into JSON (and respects the HTTP `Accept: application/json`) becomes as simple as calling your service via its published `/route` and traversing the resultset directly in JavaScript:

```js
$.getJSON("/contacts", addContacts);

function addContacts(contacts) {
    $("#ajax").append(contactsHtml(contacts));
}
```

Generating HTML via Ajax is effectively the same as **Embedded JSON** in which we're able to re-use the `contactsHtml()` method to generate the HTML, the only difference is the JSON is a result of an `$.getJSON()` ajax call instead of calling the method directly.

#### View Model

A more traditional approach to access data from within a Razor page that is familiar to MVC developers is to have it passed in as the ViewModel into the page. In ServiceStack, you don't need a separate Controller because your existing Services also serves as the Controller for views where its response is used as the ViewModel, in which case the syntax is exactly the same as it is in ASP.NET MVC, i.e:

```html
﻿@model Contact

<h3>View Model</h3>
<ul>
    <li>@Model.Name @Model.Email (@Model.Age)</li>
</ul>
```

This is the entire contents of the [/Views/GetContact.cshtml](https://github.com/ServiceStack/EmailContacts/blob/master/src/EmailContacts/Views/GetContact.cshtml) page which can be viewed at [/contacts/1](/contacts/1). 
Pages that renders the response of a Service are called **View Pages** and are maintained anywhere (i.e. any nested folder structure) in the `/Views` directory. 
The most appropriate **View Page** that gets selected is based on the following order of precedence:

  - The same name as the Request DTO - e.g. **GetContact.cshtml**
  - The same name as the Resposne DTO - e.g. **Contact.cshtml**

The Selected View (and Template) can also be changed at runtime by returning a decorated response and setting the View in a `HttpResult`:

```csharp
return new HttpResult(dto) {
    View = {viewName},
    Template = {layoutName},
};
```

Using the `[DefaultView]` Request attribute filter on a Service class or method, e.g:

```csharp
[DefaultView("{View}")]
public class MyService : Service
{
    [DefaultView("{View}")]
    public object Get(Request request) { ... }
}
```

Specified by the client as part of the request, e.g. via the QueryString for Services marked with the `[ClientCanSwapTemplates]` attribute:

```csharp
[ClientCanSwapTemplates]
public class MyService : Service { ... }
```

Which will enable the client to change the View or Template used by specifying it on the QueryString, e.g: `/contact/1?View={View}`. 
This is useful in scenarios when you want to view pages in multiple page layouts, e.g. Print Previews or Mobile optimized pages.

## API-first development

A strategy we recommend for maximizing re-use of your Services is to design them from an API-first point of view where all consumers (e.g. Desktop, Mobile and Web UIs) have equal accessibility to your services since they all consume the same published API's for all of their functionality.

For web development this means that UI logic and Error handling should ideally be done on the client with JavaScript rather than behind server-side pages which gets easily coupled to your server implementation rather than your external published APIs. Whilst this may be perceived as a restriction we've found using JavaScript ends up being a productivity and responsiveness win which is more flexible and better suited than C# in genericizing reusable functionality, reducing boilerplate, string manipulation, generating HTML views, consuming ajax services, event handling, DOM binding and manipulation and other common web programming tasks.

### ServiceStack JavaScript Utils - /js/ss-utils.js

Embedded inside **ServiceStack.dll** is a JavaScript utility library that offers a number of convenience utilities in developing javascript enhanced pages and includes nice integration with ServiceStack's validation and error handling which can be included in any page with:

```html
<script type="text/javascript" src="/js/ss-utils.js"></script>
```

To showcase how it can simplify general web development, we'll walkthrough the JavaScript needed to provide all the behavior for the [entire UI](https://github.com/ServiceStack/EmailContacts/blob/master/src/EmailContacts/default.cshtml), captured in the 70 lines of JavaScript below using nothing other than jQuery and bootstrap.js:

```js
$("input").change($.ss.clearAdjacentError);
$.getJSON("/contacts", addContacts);
refreshEmailHistory();

function addContacts(contacts) {
    var html = contacts.map(function (c) {
        return "<li data-id='" + c.Id + "' data-click='showContact'>" +
                "<span class='glyphicon glyphicon-user' style='margin: 0 5px 0 0'></span>" +
                c.Name + " " + " (" + c.Age + ")" +
                '<span class="glyphicon glyphicon-remove-circle" data-click="deleteContact"></span>'
             + "</li>";
    });
    $("#contacts").append(html.join(''));
}

function refreshEmailHistory() {
    $.getJSON("/emails", function (emails) {
        if (emails.length > 0) {
            $("#email-history").show().find("TABLE tbody").html(
                emails.map(function(email) {
                    return "<tr>" +
                        "<td>" + email.Id + "</td>" +
                        "<td>" + email.To + "</td>" +
                        "<td>" + email.Subject + "</td>" +
                    "</tr>";
                }));
        }});
}

$("#form-addcontact").bindForm({
    success: function (contact) {
        addContacts([contact]);
        $("#form-addcontact input").val('')
            .first().focus();
    }
});

$("#form-emailcontact").bindForm({
    success: function (request) {
        $("#form-emailcontact .form-control").val('')
        .parents("form").find('.alert-success')
            .html('Email was sent to ' + (request.Email || "MQ"))
            .show();
            
        refreshEmailHistory();
    }
});

$(document).bindHandlers({
    showContact: function() {
        var id = $(this).data("id");
        $.getJSON("/contacts/" + id, function (contact) {
            $("#email-contact")
                .applyValues(contact)
                .show();
            $("#form-emailcontact .alert-success").hide();
        });
    },
    deleteContact: function () {
        var $li = $(this).closest("li");
        $.post("/contacts/" + $li.data("id") + "/delete", function () {
            $li.remove();
        });
    },
    toggleAction: function() {
        var $form = $(this).closest("form"), action = $form.attr("action");
        $form.attr("action", $form.data("action-alt"))
             .data("action-alt", action);
    }
});
```

The stand-alone page also doesn't contain any images, relying instead on Bootstraps glyphicon fonts for its graphics.

### Bootstrap Forms

ServiceStack JS Utils validation and error handling support works with Bootstrap's standard HTML Form markup as seen with the HTML for the **Add Contacts** HTML Form:

```html
<form id="form-addcontact" action="@(new CreateContact().ToPostUrl())" method="POST">
    <div class="row">
        <div class="col-sm-3 form-group">
            <label for="Name">Name</label>
            <input class="form-control input-sm" type="text" id="Name" name="Name" value="">
            <span class="help-block"></span>
        </div>
        ...
    </div>
</form>
```

The first interesting thing is the **action** url is created with a typed API populated by the `ToPostUrl()` extension method that looks at `CreateContact` Request DTO to return the best matching route based on the Route definitions and the fields populated in the Request DTO instance, in this case the empty Request DTO matches `[Route("/contacts", "POST")]` so returns `/contacts`.

Other significant parts in this HTML Form is that the **INPUT** field names match up with the Request DTO it posts to and that it includes Bootstraps **class="help-block"** placeholders adjacent to each INPUT element which is what 
**ss-utils.js** uses to bind the field validation errors.

#### Binding HTML Forms

You can ajaxify this FORM by using ss-utils jQuery mixin `bindForm`, e.g:

```js
$("#form-addcontact").bindForm({
    success: function (contact) {
        addContacts([contact]);
        $("#form-addcontact input").val('')
            .first().focus();
    }
});
```

This takes over the handling of this FORM and instead of doing a POST back of the entire page to the server, makes an Ajax request using all the fields in the FORM to POST the data to the **CreateContact** Service:

```csharp
public Contact Post(CreateContact request)
{
    var contact = request.ConvertTo<Contact>();
    Db.Save(contact);
    return contact;
}
```

As seen from the implementation, the above service uses ServiceStack's built-in [AutoMapping](https://github.com/ServiceStack/ServiceStack/wiki/Auto-mapping) to Convert the `CreateContact` Request DTO into an instance of `Contact` POCO DataModel which OrmLite's `Save()` extension method will either INSERT or UPDATE depending on if the Contact already exists or not.

#### Fluent Validation

Normally the Service implementation will be called as-is but as we've added the FluentValidation `ValidationFeature` plugin and there exists a validator for `CreateContact` below:

```csharp
public class CotntactsValidator : AbstractValidator<CreateContact>
{
    public CotntactsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("A Name is what's needed.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).GreaterThan(0);
    }
}
```

The Request DTO is first validated with the above declarative rules and if it fails returns a structured error response which ss-utils uses to bind the validation errors to all the invalid field **class=help-block** (or help-inline) placeholders:

![HTML Validation](https://raw.github.com/ServiceStack/EmailContacts/master/src/EmailContacts/Content/html-validation.png)

Whilst the user goes back and corrects their INPUT, we can provide instant feedback and clear the errors as they update each each field with:

```js
$("input").change($.ss.clearAdjacentError);
```

Once all is sucessful we invoke the `success:` callback with the response of the Service which in this case is the newly created `Contact` which we dynamically add to the contacts list by calling the existing `addContacts()` method. We also clear all form values and put focus back to the first field, ready for a rapid entry of the next Contact:

```js
$("#form-addcontact").bindForm({
    success: function (contact) {
        addContacts([contact]);
        $("#form-addcontact input").val('')
            .first().focus();
    }
});
```

### Declarative Events

An interesting difference in the dynamically generated HTML are the presence of **data-click=showContact** and **data-click=deleteContact** attributes:

```js
function addContacts(contacts) {
    var html = contacts.map(function (c) {
        return "<li data-id='" + c.Id + "' data-click='showContact'>" +
                "<span class='glyphicon glyphicon-user' style='margin: 0 5px 0 0'></span>" +
                c.Name + " " + " (" + c.Age + ")" +
                '<span class="glyphicon glyphicon-remove-circle" data-click="deleteContact"></span>'
             + "</li>";
    });
    $("#contacts").append(html.join(''));
}
```

This show cases some of the declarative event support in ss-utils which allows you to invoke event handlers without needing to maintain bookkeeping of event handlers when adding or removing elements. 
You can instead define one set of event handlers for the entire page with `bindHandlers`, e.g:

```js
$(document).bindHandlers({
    showContact: function() {
        var id = $(this).data("id");
        $.getJSON("/contacts/" + id, function (contact) {
            $("#email-contact")
                .applyValues(contact)
                .show();
            $("#form-emailcontact .alert-success").hide();
        });
    },
    deleteContact: function () {
        var $li = $(this).closest("li");
        $.post("/contacts/" + $li.data("id") + "/delete", function () {
            $li.remove();
        });
    },
    toggleAction: function() {
        var $form = $(this).closest("form"), action = $form.attr("action");
        $form.attr("action", $form.data("action-alt"))
                .data("action-alt", action);
    }
});
```

The matching event handler will be invoked whenever an element with **data-click={handlerName}** is clicked. In addition to click, a number of other jQuery events can be declared in this way:

```js
$.ss.listenOn = 'click dblclick change focus blur focusin focusout select keydown keypress keyup hover toggle';
```

### Data Binding

Diving into the implementation of **showContact** we see another of ss-utils features in action with the `applyValues()` jQuery mixin which binds a JS object to the target element, in this case **#email-contact**:

```js
showContact: function() {
    var id = $(this).data("id");
    $.getJSON("/contacts/" + id, function (contact) {
        $("#email-contact")
            .applyValues(contact)
            .show();
        $("#form-emailcontact .alert-success").hide();
    });
},
```

The databinding applied by `applyValues()` include:

  - Set the **value** of all elements with matching **id={field}** or **name={field}**
  - Set the **value** of all elements marked with **data-val**
  - Set the innerHTML contents of all elements marked with **data-html**

## Advanced bindForm usages

### Form Loading

Whilst a FORM is being processed all its buttons with `[type=submit]` (overridable with `$.ss.onSubmitDisable`) are disabled and a **loading** class is added whilst a response from the server is pending. 
This can be used to provide UX feedback to end users with just CSS. E.g. we use `.loading` CSS rule to show the rotating glyphicon:

```css
#email-contact .loading .rotate {
    visibility: visible;
}
```

### Server initiated actions

Some useful functionality not demonstrated in this example is your Services ability to invoke client behavior by returning a response decorated with custom HTTP Headers. 
An example is being able to return "Soft Redirects" to navigate to a different page by adding a **X-Location** HTTP Header, e.g:

```csharp
return new HttpResult(response) {
    Headers = {
        { "X-Location", newLocationUri },
    }
};
```

When returned to a ajax form, it will instruct the page to automatically redirect to the new url.

You can also trigger an event on the page by returning a **X-Trigger** header, e.g:

```csharp
return new HttpResult(response) {
    Headers = {
        { "X-Trigger", "showLoginPopup" },
    }
};
```

In this case the page event handler named **showLoginPopup** will be invoked if it exists.

As we expect these features to be popular when developing ajax apps we've provided shorter typed aliases for the above examples:

```csharp
return HttpResult.SoftRedirect(new ViewContact { Id = newContact.Id }.ToGetUrl(), newContact);
return HttpResult.TriggerEvent(contact, eventName:"showLoginPopup");
```

## Message Queues

### Benefits of Message Queues

One of the benefits of using ServiceStack is its integrated support for hosting MQ Servers allowing your Services to be invoked via a MQ Broker. 
There are a number of reasons why you'd want to use a MQ as an alternative to HTTP including:

  - Sender is decoupled from Receiver, eliminating point-to-point coupling and configuration
  - Allows no-touch deploy of new clients and servers without updating any configuration
  - Removes time-coupling allowing clients and servers to be deployed independently without downtime
  - More reliable, consumers can still send messages when servers are down and vice-versa
  - Durable, messages can be persisted and survive application or server restarts
  - Allows for CPU Intensive or long operations without disrupting message workflow
  - Instant response times by queuing slow operations and executing them in the background
  - Message-based design allows for easier parallelization and introspection of computations
  - Allows for natural load-balancing where throughput can be increased by simply adding more processors or servers
  - Greater throttling and control of message throughput, message execution can be determined by server
  - Reduces request contention and can defer execution of high load spikes over time
  - Better recovery, messages generating server exceptions can be retried and later maintained in a dead-letter-queue
  - DLQ messages can be introspected, fixed and later replayed after server updates and rejoin normal message workflow

More details of these and other advantages can be found in the definitive [Enterprise Integration Patterns](http://www.eaipatterns.com/).

### Using an MQ for sending Emails

Sending emails is a common task that's particularly well suited for Message Queues where SMTP Servers often have resource limits and quotas that can often fail when trying to process a high volume of emails at once. Instead of building a bespoke queuing solution just for processing system emails, you can easily take advantage of purpose-built MQ Brokers to get the desired functionality for free.

ServiceStack includes support for a number of MQ options which as they all implement ServiceStack's [Messaging API](https://github.com/ServiceStack/ServiceStack/wiki/Messaging#wiki-messaging-api), are easily interchangeable.

### Rabbit MQ

For this project we'll use the industrial-strength and popular [RabbitMQ](https://www.rabbitmq.com/), documentation for Rabbit MQ support in ServiceStack can be found on the 
[Rabbit MQ wiki](https://github.com/ServiceStack/ServiceStack/wiki/Rabbit-MQ). 
Before we can use it, we need to install it first, which can be done by following the [Rabbit MQ Installation guide for Windows](https://github.com/mythz/rabbitmq-windows).

### Configuring an MQ Server in ServiceStack

Once the Rabbit MQ broker is started we can start using it. Configuring an MQ Server in ServiceStack are all done in the same way, e.g:

```csharp
container.Register<IMessageService>(c => new RabbitMqServer());
var mqServer = container.Resolve<IMessageService>();

mqServer.RegisterHandler<EmailContact>(ServiceController.ExecuteMessage);

mqServer.Start();
```

  1. Register which concrete MQ Server implementation you wish to use
  1. Register which services you wish to make available via MQ
  1. Start the MQ Server

With the above configuration ServiceStack will now let you send requests via MQ, to test this out we can go to the Integration tests which shows how to create a MQ Client and start publishing requests:

### Reliable and Durable Messaging

```csharp
var mqFactory = new RabbitMqMessageFactory();

using (var mqClient = mqFactory.CreateMessageQueueClient())
{
    mqClient.Publish(new EmailContact { ContactId = 1, Subject = "MQ Email #1", Body = "Body 1" });
    mqClient.Publish(new EmailContact { ContactId = 1, Subject = "MQ Email #2", Body = "Body 2" });
}
```

The client above publishes messages into Rabbit MQ Broker and if there is an instance of ServiceStack running, it will process each message one-by-one. We can start seeing some of the benefits of using MQs by shutting down the ServiceStack server and re-running the test code to see that messages are still being published without error. When no services are up processing messages, the messages just sit in Rabbit MQ's server-side queues until they get consumed. We can verify this by looking at the [Rabbit MQ Management UI](https://github.com/mythz/rabbitmq-windows#publishing-a-persistent-message-to-a-queue) 
and inspecting the **mq:EmailContact:inq** Inbox to see 2 pending messages:

![Rabbit MQ EmailContact Inbox](https://raw.github.com/ServiceStack/EmailContacts/master/src/EmailContacts/Content/rabbitmq-inq.png)

Now if you start the ServiceStack Server any pending messages get processed and the emails are sent.

If you tried sending the request via HTTP when the Server is offline you'll expectedly get connection exceptions notifying you that the client was unable to connect with the server and the valid client requests are lost:

```csharp
var client = new JsonServiceClient(baseUri);
client.Post(new EmailContact { ContactId=1, Subject = "HTTP Email #1", Body = "ZBody" }); //throws
```

### Deferred Execution and Instant Response Times

We can explore another benefit of using MQ's by taking advantage of 
[ServiceStack's pre-defined routes](https://github.com/ServiceStack/ServiceStack/wiki/Routing#wiki-pre-defined-routes) 
and the in-built behavior of `/oneway` routes which will automatically publish the HTTP request into the Registered MQ Server if one exists, 
if no MQ Server is registered the HTTP Request falls back to the normal behavior and is executed synchronously.

When requests are published to the registered MQ Broker the execution of that request is deferred and the response time of that request is only the time it takes to publish the Request DTO to the broker and not the execution of that request.
To see this in action we check the **Email via MQ** checkbox which just swaps the url that the form is posted to, to use the alternate `/oneway` url:

```html
<form id="form-emailcontact" method="POST"
      action="@(new EmailContact().ToPostUrl())" 
      data-action-alt="@(new EmailContact().ToOneWayUrl())">
    ...
    <input type="checkbox" id="chkAction" data-click="toggleAction" />
    <label for="chkAction">Email via MQ</label>
    ...
</form>

$(document).bindHandlers({
    ...
    toggleAction: function() {
        var $form = $(this).closest("form"), action = $form.attr("action");
        $form.attr("action", $form.data("action-alt"))
                .data("action-alt", action);
    }
});
```

If the checkbox remains unchecked you should notice the rotating glyphicon whilst the email is being sent if using the `SmtpEmailer`, otherwise if the AppHost is registered to use the `DbEmailer` you'll see the 1 second simulated delay:

```csharp
public class EmailServices : Service
{
    public IEmailer Emailer { get; set; }

    public EmailContactResponse Any(EmailContact request)
    {
        var contact = Db.SingleById<Contact>(request.ContactId);
        if (contact == null)
            throw HttpError.NotFound("Contact does not exist");

        var msg = new Email { From = "demo@servicestack.net", To = contact.Email }
            .PopulateWith(request);

        Emailer.Email(msg);

        return new EmailContactResponse { Email = contact.Email };
    }
}

public interface IEmailer
{
    void Email(Email email);
}

public class SmtpEmailer : RepositoryBase, IEmailer
{
    public SmtpConfig Config { get; set; }

    public void Email(Email email)
    {
        var msg = new MailMessage(email.From, email.To).PopulateWith(email);
        using (var client = new SmtpClient(Config.Host, Config.Port))
        {
            client.Credentials = new NetworkCredential(Config.UserName, Config.Password);
            client.EnableSsl = true;
            client.Send(msg);
        }

        Db.Save(email);
    }
}

public class DbEmailer : RepositoryBase, IEmailer
{
    public void Email(Email email)
    {
        Thread.Sleep(1000);  //simulate processing delay
        Db.Save(email);
    }
}
```

If the checkbox is checked it will use the alternative `/oneway` url and sending emails becomes instant. 
You'll also not see the message appear in the **Email History** right away as the history gets refreshed before the operation had completed. 
Sending another email or a manual `F5` refresh will refresh message history and see notification of the email being sent.

## Integration Tests

ServiceStack's end-to-end typed API simplifies integration testing and introspection of live services letting you use clean, typed, terse, sync or async C# code. You can use any one of the many 
[.NET Service Clients](https://github.com/ServiceStack/ServiceStack/wiki/C%23-client) available, initialized with the base url of where your ServiceStack services are hosted, e.g:

```csharp
[TestFixture]
public class IntegrationTests
{
    readonly IServiceClient client = new JsonServiceClient("http://localhost:64077/");

    [Test]
    public void Can_call_with_JsonServiceClient()
    {
        client.Post(new CreateContact { 
            Name = "Unit Test", Email = "demo+unit@servicestack.net", Age = 27 });

        Contact contact = client.Get(new GetContact { Id = 1 });

        "GetContact: ".Print();
        contact.PrintDump();

        List<Contact> response = client.Get(new FindContacts { Age = 27 });

        "FindContacts: ".Print();
        response.PrintDump();

    }

    [Test]
    public async Task Can_call_with_JsonServiceClient_Async()
    {
        List<Contact> response = await client.GetAsync(new FindContacts { Age = 27 });
        response.PrintDump();
    }

    [Test]
    public void Does_throw_on_invalid_requests()
    {
        try
        {
            client.Post(new EmailContact { ContactId = -1, Subject = "Test" });
            Assert.Fail("Should throw");
        }
        catch (WebServiceException ex)
        {
            Assert.That(ex.StatusCode, Is.EqualTo(404));
            Assert.That(ex.ResponseStatus.Message, Is.EqualTo("Contact does not exist"));
        }
    }
}
```

Remember to use a `async Task` return type when testing Async APIs that use `await` on the async Task responses.

The `T.PrintDump()` extension method writes out a recursive, pretty-formatted dump of all the response DTO's properties which we find to be an invaluable time-saver for introspecting responses of live services.

## Unit Tests

A nice characteristic of ServiceStack services are that they are plain C# classes which have all their dependencies injected which makes calling them as simple as resolving them from the IOC and calling their C# methods. They also share a similar structure to real services where all configuration and dependencies are registered in an AppHost and Services are executed using the same Request DTO's used in live services and integration tests.

To simplify the environment for unit testing you can use ServiceStack's `BasicAppHost` class which is an open-ended / implementation-agnostic AppHost base class that's not coupled to any HTTP Server but still retains the same functionality as it shares the same `ServiceStackHost` base class.

As its open-ended it provides a number of custom hooks to be able to configure the test context without needing to create multiple test AppHosts. In this Unit Test example we're using an in-memory Sqlite database that's seeded with only 1 test contact that's used for all the tests:

```csharp
[TestFixture]
public class UnitTests
{
    private readonly ServiceStackHost appHost;

    public UnitTests()
    {
        appHost = new BasicAppHost(typeof(EmailServices).Assembly)
        {
            ConfigureContainer = container =>
            {
                container.Register<IDbConnectionFactory>(c =>
                    new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

                container.RegisterAs<DbEmailer, IEmailer>();

                using (var db = container.TryResolve<IDbConnectionFactory>().Open())
                {
                    db.DropAndCreateTable<Contact>();
                    db.DropAndCreateTable<Email>();

                    db.Insert(new Contact { 
                        Name = "Test Contact", Email = "test@email.com", Age = 10 });
                }
            }
        }
        .Init();
    }

    [TestFixtureTearDown]
    public void TestFixtureTearDown()
    {
        appHost.Dispose();
    }

    [Test]
    public void Can_send_Email_to_TestContact()
    {
        using (var db = appHost.TryResolve<IDbConnectionFactory>().Open())
        using (var service = appHost.TryResolve<EmailServices>())
        {
            var contact = db.Single<Contact>(q => q.Email == "test@email.com");

            var response = service.Any(
                new EmailContact { ContactId = contact.Id, Subject = "Test Subject" });

            Assert.That(response.Email, Is.EqualTo(contact.Email));

            var email = db.Single<Email>(q => q.To == contact.Email);

            Assert.That(email.Subject, Is.EqualTo("Test Subject"));
        }
    }

    [Test]
    public void Does_throw_when_sending_to_invalid_Contact()
    {
        using (var service = appHost.TryResolve<EmailServices>())
        {
            Assert.Throws<HttpError>(() =>
                service.Any(new EmailContact { ContactId = -1 }));
        }
    }
}
```

## Further Reading

  - For more tutorials see the [getting started tutorials and walkthroughs from the ServiceStack community](https://github.com/ServiceStack/ServiceStack/wiki/Create-your-first-webservice#wiki-community-resources)
  - For more example projects see the [Definitive list of Examples, Use-Cases, Demos, Starter Templates](http://stackoverflow.com/a/15869816)
  - For available courses on ServiceStack see [the ServiceStack courses on Plural Sight](http://pluralsight.com/training/Courses/Find?highlight=true&searchTerm=servicestack)
 

 