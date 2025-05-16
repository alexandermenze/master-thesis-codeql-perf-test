/**
 * @name Identifies connections between call points including tag methods
 * @kind table
 * @id csharp/identify-call-point-connections
 */

import csharp
import callpoints
import callgraph
import externalfilter
import mappings

query predicate isSource(Callable c) { c instanceof CallPoints::InboundCallPoint }

query predicate isSink(Callable c) { c instanceof CallPoints::OutboundCallPoint }

query predicate edgesWithCall(Callable source, Callable sink, Call c) {
  c.getARuntimeTarget() = sink and
  c.getEnclosingCallable() = source
  or
  exists(ConstructedGeneric cg | cg.getUnboundGeneric() = sink and edgesWithCall(source, cg, c))
}

string findDataflowName(Callable source, Method sink) {
  result =
    concat(Callable between, Method tagMethod, Call c |
      CallGraph::edgesWithoutTag+(source, between) and
      CallGraph::edgesWithoutTag+(between, sink) and
      Mappings::isDataflowTagMethod(tagMethod) and
      c.getTarget() = tagMethod and
      c.getAnArgument() = between
    |
      c.getArgument(0).getValue(), "|" order by CallGraph::distanceTo(source, between)
    )
}

string findDataflowMethodNameOrEmpty(Callable source, Method sink) {
  result =
    concat(Callable between, Method tagMethod |
      CallGraph::edgesWithoutTag+(source, between) and
      CallGraph::edgesWithoutTag+(between, sink) and
      Mappings::isDataflowTagMethod(tagMethod) and
      exists(Call c | c.getTarget() = tagMethod and c.getAnArgument() = between)
    |
      tagMethod.getName(), "|" order by CallGraph::distanceTo(source, between)
    )
}

from
  Callable source, Method sinkCaller, Method sink, string processName, string dataflowName,
  string dataflowMethodName
where
  not ExternalFilter::isFilteredMethod(sink) and
  not Mappings::isDataflowTagMethod(source) and
  not Mappings::isDataflowTagMethod(sinkCaller) and
  not Mappings::isDataflowTagMethod(sink) and
  not exists(Method m | sink = m |
    m.getOverridee*().hasFullyQualifiedName("System.Object", "ToString") or
    m.getOverridee*().hasFullyQualifiedName("System.Object", "GetHashCode") or
    m.getOverridee*().hasFullyQualifiedName("System.Object", "Equals")
  ) and
  not sink instanceof UnboundGeneric and
  isSource(source) and
  isSink(sink) and
  CallGraph::edgesWithoutTag+(source, sink) and
  CallGraph::edgesWithoutTag*(source, sinkCaller) and
  CallGraph::isClosestMethod(sinkCaller, sink) and
  processName = Mappings::getProcessNameOrEmpty(source) and
  dataflowName = findDataflowName(source, sink) and
  dataflowMethodName = findDataflowMethodNameOrEmpty(source, sink)
select source.getFullyQualifiedNameDebug() as entrypoint, sinkCaller.getFullyQualifiedNameDebug() as internalcall,
  sink.getFullyQualifiedNameDebug() as externalcall, processName, dataflowName, dataflowMethodName
