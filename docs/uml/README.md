# UML Diagram Sources

These files hold the **source** of every diagram in the SCAIS design document, plus the
checklist needed to redraw each one in Sparx Enterprise Architect.

---

## 1. Why the sources are here rather than only in EA

Enterprise Architect stores its model in a proprietary `.eapx` file, which cannot be
generated from outside EA and cannot be diffed or reviewed. The diagrams are therefore
written in **Mermaid**, which is plain text, renders directly on GitHub, and can be
reviewed and version controlled like the rest of the project.

**The Mermaid diagrams are the specification. The EA file is the submission.**

When redrawing in EA, use the Mermaid diagram for the shape of the model and the
checklist at the bottom of each file for the detail that must not be missed —
multiplicities, visibility markers and relationship types.

---

## 2. Diagram index

| # | File | Diagram | Rubric marks |
|---|---|---|---|
| 1 | [`01-use-case-diagram.md`](01-use-case-diagram.md) | Use case diagram — 3 actors, 19 use cases | **4** |
| 2 | [`02-class-diagram.md`](02-class-diagram.md) | Class diagram — the domain model and the services | **15** |
| 3 | [`03-sequence-diagrams.md`](03-sequence-diagrams.md) | Four sequence diagrams | **4** |
| 4 | [`04-erd.md`](04-erd.md) | Entity relationship diagram — 13 tables | part of **10** |
| 5 | [`05-design-patterns.md`](05-design-patterns.md) | Singleton, Observer and Strategy structures | **20** |
| 6 | [`06-state-diagram.md`](06-state-diagram.md) | The course plan lifecycle | supporting |

Consistency between these diagrams is worth **2 marks** on its own, and the checklist in
[`07-consistency-checklist.md`](07-consistency-checklist.md) is what protects it.

---

## 3. How to redraw in Enterprise Architect

For each diagram:

1. Create a package under the project root, for example `SCAIS / Design / Class Model`.
2. Add the diagram of the matching type (`Add Diagram…` → UML Structural for class and
   component, UML Behavioural for use case and sequence).
3. Add the elements using the tables in each file. Set the **visibility** of every
   attribute and operation — this is one of the most commonly lost marks.
4. Add the relationships and set **both multiplicities** on every association.
5. Export the diagram (`Publish → Save as Image`) at a readable resolution and paste it
   into the report.

### Naming

Element names in EA must match the code exactly, because the report claims the diagrams
and the implementation agree. The names used throughout are the real class names from
`src/SCAIS`.

### Rendering the Mermaid sources locally

The diagrams render automatically when these files are viewed on GitHub. To produce a
PNG locally, either

- use the VS Code **Markdown Preview Mermaid Support** extension, or
- use the command line, for example
  `npx @mermaid-js/mermaid-cli -i 02-class-diagram.md -o class-diagram.png`.

---

## 4. Diagram conventions used throughout

| Convention | Meaning |
|---|---|
| `-` | private member |
| `#` | protected member |
| `+` | public member |
| `~` | package or internal member |
| `«abstract»` | the class cannot be instantiated |
| `«interface»` | the element is a contract with no implementation |
| `«Singleton»` | the class enforces a single instance |
| `*` suffix on an operation | the operation is abstract and must be overridden |
| `1 — 0..*` | multiplicity, written on **both** ends of every association |

---

## 5. The counts the report quotes

| Quantity | Value |
|---|---|
| Actors | 3 |
| Use cases | 19 |
| Domain classes | 13 |
| Service and data classes | 15 |
| Interfaces | 3 |
| Sequence diagrams | 4 |
| Database tables | 13 |
| Design patterns | 3 (Singleton, Observer, Strategy) |
| Reports | 2 |
