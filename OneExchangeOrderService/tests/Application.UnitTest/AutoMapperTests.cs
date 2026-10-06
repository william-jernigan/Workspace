#pragma warning disable IDE0005 // Using directive is unnecessary.
#pragma warning restore IDE0005 // Using directive is unnecessary.
using AutoMapper;

namespace Application.UnitTests
{
    [TestClass]
    public class AutoMapperTests
    {
        private readonly MapperConfiguration _configuration;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly IMapper _mapper;
#pragma warning restore IDE0052 // Remove unread private members

        public AutoMapperTests()
        {
            _configuration = new MapperConfiguration(config =>
            {
                //TimeshareContract entity example
                //config.AddProfile<TimeShareContracts.Api.Enterprise.Application.Chorus.Mappings.ContractProfile>();
                //config.AddProfile<TimeShareContracts.Api.Enterprise.Application.Clarity.Mappings.ContractProfile>();
            });

            _mapper = _configuration.CreateMapper();
        }

        [TestMethod]
        public void ShouldHaveValidConfiguration() => _configuration.AssertConfigurationIsValid();

        //Mapping test example
        //[TestMethod]
        //[DataRow(typeof(ContractsInventoryContractEntity), typeof(TimeShareContract))]
        //[DataRow(typeof(TimeshareContractEntity), typeof(TimeShareContract))]
        //public void ShouldMapFromSourceToDestination(Type source, Type destination)
        //{
        //    var instance = GetInstanceOf(source);
        //    _mapper.Map(instance, source, destination);
        //}

        //private static object GetInstanceOf(Type type)
        //{
        //    if (type.GetConstructor(Type.EmptyTypes) != null)
        //    {
        //        return Activator.CreateInstance(type);
        //    }

        //    return FormatterServices.GetUninitializedObject(type);
        //}
    }
}
