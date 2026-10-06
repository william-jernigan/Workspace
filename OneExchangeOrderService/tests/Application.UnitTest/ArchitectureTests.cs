using NetArchTest.Rules;

namespace Application.UnitTests
{
    [TestClass]
    public class ArchitectureTests
    {
        private const string DOMAIN_NAMESPACE = "OneExchangeOrderService.Domain";
        private const string BUSINESS_NAMESPACE = "OneExchangeOrderService.Application";
        private const string INFRASTRUCTURE_NAMESPACE = "OneExchangeOrderService.Infrastructure";
        private const string API_NAMESPACE = "OneExchangeOrderService.WebApi";

        [TestMethod]
        public void DomainShouldNotHaveProjectDependency()
        {
            // Arrange
            var otherProjects = new[] { BUSINESS_NAMESPACE, INFRASTRUCTURE_NAMESPACE, API_NAMESPACE };

            // Act
            var testResult = Types.InNamespace(DOMAIN_NAMESPACE)
                                    .ShouldNot()
                                    .HaveDependencyOnAll(otherProjects)
                                    .GetResult();

            // Assert
            Assert.IsTrue(testResult.IsSuccessful);
        }

        [TestMethod]
        public void BusinessShouldNotHaveInfrastructureOrAPIProjectDependency()
        {
            // Arrange
            var otherProjects = new[] { INFRASTRUCTURE_NAMESPACE, API_NAMESPACE };

            // Act
            var testResult = Types.InNamespace(BUSINESS_NAMESPACE)
                                    .ShouldNot()
                                    .HaveDependencyOnAll(otherProjects)
                                    .GetResult();

            // Assert
            Assert.IsTrue(testResult.IsSuccessful);
        }

        [TestMethod]
        public void InfrastructureShouldNotHaveAPIProjectDependency()
        {
            // Arrange
            var otherProjects = new[] { API_NAMESPACE };

            // Act
            var testResult = Types.InNamespace(INFRASTRUCTURE_NAMESPACE)
                                    .ShouldNot()
                                    .HaveDependencyOnAll(otherProjects)
                                    .GetResult();

            // Assert
            Assert.IsTrue(testResult.IsSuccessful);
        }

    }
}
