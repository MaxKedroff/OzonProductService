using Application.DTOs;
using AutoMapper;
using Domain.Models;
using Domain.ValueObjects;


namespace Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Product, ProductResponseDto>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Price.Currency))
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => src.Sku.Value));

            CreateMap<ProductFilterDto, ProductFilter>()
            .ForMember(dest => dest.SearchTerm, opt => opt.MapFrom(src => src.Search))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.MinPrice, opt => opt.MapFrom(src => src.MinPrice))
            .ForMember(dest => dest.MaxPrice, opt => opt.MapFrom(src => src.MaxPrice))
            .ForMember(dest => dest.Page, opt => opt.MapFrom(src => src.Page))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PageSize))
            .ForMember(dest => dest.SortBy, opt => opt.MapFrom(src => src.SortBy))
            .ForMember(dest => dest.SortDescending, opt => opt.MapFrom(src => src.SortDescending));
        }
    }
}
