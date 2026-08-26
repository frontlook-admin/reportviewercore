# RDLC designer document model compatibility

`RdlcDocument` is the design-time parsing and preservation boundary. It parses the report root, report sections/body/report items, data sources, datasets, and report parameters into typed read-only views while retaining the original `XDocument` (including namespace declarations, unknown elements, attributes, and source ordering) for deterministic serialization.

## Supported foundation

- RDLC/XML documents with a `Report` root in any namespace, including the current 2008/01, 2010/01, 2016/01, and custom extension namespaces.
- Multiple report sections, data sources, datasets, parameters, and arbitrary report items.
- Unknown report-level, section-level, body-level, and item-level XML is preserved and exposed through `Extensions`/`Xml` views. Advanced authoring adds chart/subreport shells, bookmarks, drillthrough actions, interactive sorting, toggle items, and loss-preserving Map/GaugePanel extension items without pretending to render vendor-specific semantics.
- Existing whitespace and XML declaration are preserved as far as `XDocument` permits; `ToXml()` uses non-formatting serialization for stable load-save-load output.

## Compatibility boundary

The design model does not interpret renderer-specific chart/map/gauge styling or execute drillthrough/subreport targets. It writes the standard authoring XML seams and validates local identifiers, dataset references, and action values; unsupported extension payloads remain lossless XML. A missing `Report` root and malformed XML are rejected with `RdlcDocumentFormatException`; a syntactically valid but semantically unknown item is retained rather than rejected.
