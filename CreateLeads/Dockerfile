#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM registry.access.redhat.com/ubi8/dotnet-60:6.0 as builder
WORKDIR /opt/app-root/src

COPY --chown=1001 . .
RUN dotnet publish "CreateLeads/CreateLeads.csproj" -c Release


FROM registry.access.redhat.com/ubi8/dotnet-60:6.0
EXPOSE 8081
ENV ASPNETCORE_URLS=http://*:8081
COPY --from=builder /opt/app-root/src/CreateLeads/bin /opt/app-root/src/bin
WORKDIR /opt/app-root/src/bin/Release/net6.0/publish
CMD ["dotnet", "CreateLeads.dll"]