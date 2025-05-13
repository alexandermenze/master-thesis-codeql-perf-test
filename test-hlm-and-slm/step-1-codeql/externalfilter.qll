module ExternalFilter {
  import csharp

  external predicate methodFilter(string namespace, string type, string name);

  predicate isFilteredMethod(Method m) {
    exists(string namespace, string type, string name |
      methodFilter(namespace, type, name) and
      m.getDeclaringType().getNamespace().getFullName().matches(namespace) and
      m.getDeclaringType().getName().matches(type) and
      m.getName().matches(name)
    )
  }
}
