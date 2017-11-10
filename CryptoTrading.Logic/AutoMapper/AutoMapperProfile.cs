using System;
using AutoMapper;
using CryptoTrading.DAL.Models;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Providers.Models;

namespace CryptoTrading.Logic.AutoMapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<CandleDto, CandleModel>();
            CreateMap<CandleModel, CandleDto>();

            CreateMap<KrakenOhlc,CandleModel>();
            CreateMap<PoloniexCandle, CandleModel>()
                .ForMember(dest => dest.StartDateTime, opt => opt.MapFrom(src => DateTimeOffset.FromUnixTimeSeconds(src.Date).DateTime))
                .ForMember(dest => dest.ClosePrice, opt => opt.MapFrom(src => src.Close))
                .ForMember(dest => dest.OpenPrice, opt => opt.MapFrom(src => src.Open))
                .ForMember(dest => dest.HighPrice, opt => opt.MapFrom(src => src.High))
                .ForMember(dest => dest.LowPrice, opt => opt.MapFrom(src => src.Low))
                .ForMember(dest => dest.Volume, opt => opt.MapFrom(src => src.Volume))
                .ForMember(dest => dest.VolumeWeightedPrice, opt => opt.MapFrom(src => src.WeightedAverage));
        }
    }
}
