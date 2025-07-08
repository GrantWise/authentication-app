#!/bin/bash

# Fix RegisterHandlerTests - Replace PropertyName with TestHelpers method
sed -i 's/exception\.PropertyName/TestHelpers.GetFieldFromValidationException(exception)/g' AuthenticationApi.Tests/Features/Authentication/Register/RegisterHandlerTests.cs

# Fix audit logging calls in RegisterHandlerTests - add missing parameter
sed -i '88s/"127.0.0.1",/"127.0.0.1",\n            It.IsAny<string>(),/' AuthenticationApi.Tests/Features/Authentication/Register/RegisterHandlerTests.cs
sed -i '124s/"127.0.0.1",/"127.0.0.1",\n            It.IsAny<string>(),/' AuthenticationApi.Tests/Features/Authentication/Register/RegisterHandlerTests.cs
sed -i '162s/"127.0.0.1",/"127.0.0.1",\n            It.IsAny<string>(),/' AuthenticationApi.Tests/Features/Authentication/Register/RegisterHandlerTests.cs
sed -i '273s/"127.0.0.1",/"127.0.0.1",\n            It.IsAny<string>(),/' AuthenticationApi.Tests/Features/Authentication/Register/RegisterHandlerTests.cs

echo "Fixes applied!"