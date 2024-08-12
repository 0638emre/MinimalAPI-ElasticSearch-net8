using Bogus;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

//elasticsearch npm > Elastic.Clients.Elasticsearch
ElasticsearchClientSettings settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"));
settings.DefaultIndex("products");

ElasticsearchClient client = new ElasticsearchClient(settings);

client.IndexAsync("products").GetAwaiter().GetResult();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/products/create", async (CreateProductDto request, CancellationToken token) =>
{
    Product product = new()
    {
        Name = request.Name,
        Price = request.Price,
        Stock = request.Stock,
        Description = request.Description
    };

    CreateRequest<Product> createRequest = new(product.Id.ToString())
    {
        Document = product
    };

    CreateResponse createResponse = await client.CreateAsync(createRequest, token);

    return Results.Ok(createResponse.Id);
});

app.MapPut("/products/update", async (UpdateProductDto request, CancellationToken token) =>
{
    UpdateRequest<Product, UpdateProductDto> createRequest = new("products", request.Id.ToString())
    {
        Doc = request
    };

    UpdateResponse<Product> updateResponse = await client.UpdateAsync(createRequest, token);

    return Results.Ok(updateResponse);
});

app.MapDelete("/products/deleteById", async (Guid id, CancellationToken token) =>
{
    DeleteRequest createRequest = new("products", id.ToString());
    
    DeleteResponse deleteResponse = await client.DeleteAsync(createRequest, token);

    return Results.Ok(new {message= id + " succesfully deleted."});
});

app.MapGet("/products/getAll", async (CancellationToken token) =>
{
    SearchResponse<Product> response = await client.SearchAsync<Product>("products", token);
    
    return Results.Ok(response.Documents);
});

//a-z
app.MapGet("/products/getAllWithSorts", async (CancellationToken token) =>
{
    SearchRequest searchRequest = new("products")
    {
        Size = 100, //kaç data getireceği
        Sort = new List<SortOptions>()
        {
            SortOptions.Field(new Field("name.keyword"), new FieldSort() {Order = SortOrder.Asc})
        }
    };
    
    SearchResponse<Product> response = await client.SearchAsync<Product>(searchRequest, token);
    
    return Results.Ok(response.Documents);
});

//a-z and search types 
app.MapGet("/products/getAllWithSorts", async (CancellationToken token) =>
{
    //NOT: Match query > datanın stringini tam olarak arar
    //wildcardquery > like sorgusu gibi arar
    //fuzzy query > hatalı bir harf ya da yanlış bir kelime yazsan bile arar
    //bool query > aramayı sıraya koyar, önce adına göre sonra descriptiona göre gibi..
    
    SearchRequest searchRequest = new("products")
    {
        Size = 100, //kaç data getireceği
        Sort = new List<SortOptions>()
        {
            SortOptions.Field(new Field("name.keyword"), new FieldSort() {Order = SortOrder.Asc})
        },
        // Query = new MatchQuery(new Field("name"))
        // {
        //     Query = "domates"
        // },
        // Query = new WildcardQuery(new Field("name"))
        // {
        //     Value = "*domates*"
        // },
        // Query = new FuzzyQuery(new Field("name"))
        // {
        //     Value = "domtaes"
        // },
        // Query = new BoolQuery
        // {
        //     Should = new Query[]
        //     {
        //         new MatchQuery(new Field("name"))
        //         {
        //             Query = "domates"
        //         },
        //         new FuzzyQuery(new Field("description"))
        //         {
        //             Value = "aicklama"
        //         }
        //     }
        // },
    };
    
    SearchResponse<Product> response = await client.SearchAsync<Product>(searchRequest, token);
    
    return Results.Ok(response.Documents);
});

//seeddata npm-bogus
app.MapGet("/products/seeddata", async (CancellationToken token) =>
{
    for (int i = 0; i < 100; i++)
    {
        Faker faker = new();
        Product product = new()
        {
            Name = faker.Commerce.ProductName(),
            Price = Convert.ToDecimal(faker.Commerce.Price()),
            Stock = faker.Commerce.Random.Int(1,20),
            Description = faker.Commerce.ProductDescription()
        };

        CreateRequest<Product> createRequest = new(product.Id)
        {
            Document = product
        };

        CreateResponse createResponse = await client.CreateAsync(createRequest, token);
    }
    
    return Results.Ok("Seed datas loaded.");
});

app.Run();


class Product
{
    public Product()
    {
        Id = Guid.NewGuid();
    }
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Description { get; set; } = default!;
}

record CreateProductDto(string Name, decimal Price, int Stock, string Description);
record UpdateProductDto(Guid Id, string Name, decimal Price, int Stock, string Description);