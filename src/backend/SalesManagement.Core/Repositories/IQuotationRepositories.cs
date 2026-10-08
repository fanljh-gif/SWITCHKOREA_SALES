using SalesManagement.Core.Models;
namespace SalesManagement.Core.Repositories;
public interface IInquiryRepository : IRepository<CustomerInquiry> { Task<int> NextSequenceAsync(int year, int month, CancellationToken ct = default); }
public interface IQuoteRepository : IRepository<Quote> { Task<int> NextSequenceAsync(int year, int month, CancellationToken ct = default); }
public interface IIncotermRepository : IRepository<Incoterm> { Task<Incoterm?> FindByCodeAsync(string code, CancellationToken ct = default); }
