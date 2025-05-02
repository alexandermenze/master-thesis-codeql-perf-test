/**
 * @name Identifies connections between call points
 * @kind table
 * @id csharp/identify-call-point-connections
 */

import csharp
import callpoints

query predicate isSource(Callable c) { c instanceof CallPoints::InboundCallPoint }

query predicate isSink(Callable c) { c instanceof CallPoints::OutboundCallPoint }

query predicate edges(Callable source, Callable sink) {
  source.calls(sink)
  or
  exists(ConstructedGeneric cg | cg.getUnboundGeneric() = sink and source.calls(cg))
  or
  sink.getEnclosingCallable() = source
  or
  sink.getACall().getEnclosingCallable() = source
}

query predicate isClosestMethod(Method caller, Callable callee) {
  edges+(caller, callee) and
  not exists(Method between | edges+(caller, between) and edges+(between, callee))
}

from Callable source, Method sinkCaller, Method sink
where
  not exists(Method m | sink = m |
    m.getOverridee*().hasFullyQualifiedName("System.Object", "ToString") or
    m.getOverridee*().hasFullyQualifiedName("System.Object", "GetHashCode") or
    m.getOverridee*().hasFullyQualifiedName("System.Object", "Equals")
  ) and
  not sink instanceof UnboundGeneric and
  isSource(source) and
  isSink(sink) and
  edges+(source, sink) and
  edges*(source, sinkCaller) and
  isClosestMethod(sinkCaller, sink)
select source.getFullyQualifiedNameDebug(), sinkCaller.getFullyQualifiedNameDebug(),
  sink.getFullyQualifiedNameDebug()
