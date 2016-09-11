# Sushi.AspNetCore.Proxy

This is Asp.NET Core middleware with a simple goal: let Kestrel act as a Proxy server.

_Unfortunately proxying HTTPS traffic is currently not supported._

## Usage

Use UseProxy extension method from IApplicationBuilder in your Configure method:

```
public void Configure(IApplicationBuilder app)
{
    ...
    app.UseProxy();
    ...
}
```
