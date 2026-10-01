using SalesManagement.Core.Auth;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Products;

public sealed record ProductSaveCommand(Guid? Id, string Code, string Name, ItemType ItemType, string Specification, EntityStatus Status);

public sealed class ProductService(IProductRepository products)
{
    public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) => products.ListAsync(cancellationToken);

    public async Task<Product> SaveAsync(ProductSaveCommand command, User actor, long expectedVersion, CancellationToken cancellationToken = default)
    {
        AuthorizationService.EnsureManager(actor);
        if (command.Id is null) return await products.AddAsync(new Product { Code = command.Code, Name = command.Name, ItemType = command.ItemType, Specification = command.Specification, Status = command.Status }, cancellationToken);
        var product = await products.GetAsync(command.Id.Value, cancellationToken) ?? throw new KeyNotFoundException();
        product.Code = command.Code; product.Name = command.Name; product.ItemType = command.ItemType; product.Specification = command.Specification; product.Status = command.Status;
        return await products.UpdateAsync(product, expectedVersion, cancellationToken);
    }
}
