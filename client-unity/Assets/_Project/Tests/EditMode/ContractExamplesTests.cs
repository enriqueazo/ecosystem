using System;
using System.Collections.Generic;
using Ecosystem.Contracts;
using Ecosystem.Tests.EditMode.Generated;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Ecosystem.Tests.EditMode
{
    /// <summary>
    /// Los validadores C# generados deben aceptar los ejemplos válidos y rechazar los inválidos de
    /// cada contrato, igual que Ajv en el servidor (spec 003, FR-011; ADR-0010). El contrato de
    /// prueba keyword_coverage ejercita todas las palabras clave admitidas.
    /// </summary>
    public sealed class ContractExamplesTests
    {
        private static IEnumerable<TestCaseData> Cases()
        {
            foreach (var c in new[]
            {
                (Id: ProtocolVersionContract.Id,
                    Request: (Func<JToken, bool>)ProtocolVersionContract.ValidateRequest,
                    Response: (Func<JToken, bool>)ProtocolVersionContract.ValidateResponse,
                    ProtocolVersionContract.Examples.ValidRequests, ProtocolVersionContract.Examples.InvalidRequests,
                    ProtocolVersionContract.Examples.ValidResponses, ProtocolVersionContract.Examples.InvalidResponses),
                (Id: KeywordCoverageContract.Id,
                    Request: (Func<JToken, bool>)KeywordCoverageContract.ValidateRequest,
                    Response: (Func<JToken, bool>)KeywordCoverageContract.ValidateResponse,
                    KeywordCoverageContract.Examples.ValidRequests, KeywordCoverageContract.Examples.InvalidRequests,
                    KeywordCoverageContract.Examples.ValidResponses, KeywordCoverageContract.Examples.InvalidResponses),
            })
            {
                foreach (var json in c.ValidRequests) yield return Case(c.Id, "request", true, c.Request, json);
                foreach (var json in c.InvalidRequests) yield return Case(c.Id, "request", false, c.Request, json);
                foreach (var json in c.ValidResponses) yield return Case(c.Id, "response", true, c.Response, json);
                foreach (var json in c.InvalidResponses) yield return Case(c.Id, "response", false, c.Response, json);
            }
        }

        private static TestCaseData Case(string id, string part, bool valid, Func<JToken, bool> validate, string json)
        {
            return new TestCaseData(validate, json, valid)
                .SetName($"{id}.{part}.{(valid ? "valid" : "invalid")}: {json}");
        }

        [TestCaseSource(nameof(Cases))]
        public void Example_HasExpectedValidity(Func<JToken, bool> validate, string json, bool expected)
        {
            Assert.That(validate(JToken.Parse(json)), Is.EqualTo(expected), json);
        }

        [Test]
        public void ProtocolVersion_ComesFromSharedContracts()
        {
            Assert.That(ProtocolContracts.ProtocolVersion, Is.GreaterThanOrEqualTo(1));
        }
    }
}
