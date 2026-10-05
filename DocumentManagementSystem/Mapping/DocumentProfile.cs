using AutoMapper;
using Domain.Model;
using Domain.Model.DTO;

namespace DocumentManagementSystem.Mapping
{
    public class DocumentProfile:Profile
    {
        public DocumentProfile()
        {
            CreateMap<DTODocument, Document>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.UploadedAt, o => o.MapFrom(_ => DateTime.UtcNow));
        }
    }
}
