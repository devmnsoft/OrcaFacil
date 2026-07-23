namespace OrcaFacil.Web;
public static class HttpContextTrace { public static string Current(HttpContext context) => context.TraceIdentifier; }
