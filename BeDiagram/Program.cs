using BeDiagram.Components;
using BeDiagram.Components.Pages.Commands;
using BeDiagram.Services;
using BeDiagram.Services.Export;
using BeDiagram.Services.Export.Pdf;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    // The browser may send a whole diagram at once (JSON pasted into Edit as JSON). SignalR's default limit of 32 KB
    // would drop the connection for anything but a small diagram; this is a local app, so allow up to 16 MB.
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 16 * 1024 * 1024);

// Diagrams are stored as JSON files in a local folder (see appsettings.json -> Storage:Folder).
builder.Services.AddSingleton<DiagramStore>();
builder.Services.AddSingleton<StylePresetStore>();   // saved styles, shared by every diagram (styles.json)
builder.Services.AddSingleton<StyleClipboard>();     // the copied style (Copy style, format painter): every tab and window

// Export strategies (see Services/Export). The Export menu lists destinations, formats and areas in the order registered here.
// Destinations: where it goes (a file next to the diagram, or the clipboard). Formats: what kind of file.
// Areas: which part of the drawing (whole page / extents, or the current view).
builder.Services.AddSingleton<IExportDestination, FileDestination>();
builder.Services.AddScoped<IExportDestination, ClipboardDestination>();   // scoped: this user's browser clipboard
builder.Services.AddScoped<IClipboard, BrowserClipboard>();
builder.Services.AddSingleton<SvgDiagramWriter>();   // the shared SVG renderer behind SVG, PNG and PDF
builder.Services.AddSingleton<PdfWriter>();          // SVG → PDF
builder.Services.AddSingleton<IExportFormat, SvgExportFormat>();
builder.Services.AddScoped<IExportFormat, PngExportFormat>();     // scoped: draws in this user's browser
builder.Services.AddSingleton<IExportFormat, PdfExportFormat>();
builder.Services.AddSingleton<IExportFormat, MermaidExportFormat>();
builder.Services.AddSingleton<IExportArea, WholeDrawingArea>();
builder.Services.AddSingleton<IExportArea, CurrentViewArea>();
builder.Services.AddScoped<IPngRasterizer, BrowserPngRasterizer>();
builder.Services.AddScoped<ExportService>();

// Editor commands (see Components/Pages/Commands): every keyboard shortcut and the toolbar / panel actions.
builder.Services.AddSingleton<CommandRegistry>(CommandRegistry.CreateDefault());

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
