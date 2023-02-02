using System.Reflection;
using AutoMapper;

namespace IndyPOS.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
		public MappingProfile()
		{
			ApplyMappingsFromAssembly(Assembly.GetExecutingAssembly());
		}

		private void ApplyMappingsFromAssembly(Assembly assembly)
		{
			//TODO
		}
    }
}
