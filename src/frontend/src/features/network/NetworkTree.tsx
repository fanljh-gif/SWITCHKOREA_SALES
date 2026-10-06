import { useState } from 'react'
import type { NetworkTreeNode } from '../../shared/api/contracts'

function TreeNode({ node, selectedId, onSelect }: { node: NetworkTreeNode; selectedId?: string; onSelect: (id: string) => void }) {
  const [expanded, setExpanded] = useState(true)
  const hasChildren = node.children.length > 0
  const type = node.company.isOem ? '원청' : node.company.lifecycleType === 'customer' ? '정식 거래처' : '잠재 협력사'
  return <li data-company-id={node.company.id}>
    <div className={`network-node ${selectedId === node.company.id ? 'is-selected' : ''}`}>
      {hasChildren ? <button className="tree-toggle" type="button" aria-label={`${node.company.companyName} ${expanded ? '접기' : '펼치기'}`} onClick={() => setExpanded((value) => !value)}>{expanded ? '−' : '+'}</button> : <span className="tree-spacer" />}
      <button className="network-company-button" type="button" onClick={() => onSelect(node.company.id)}><strong>{node.company.companyName}</strong><span>{type}</span><small>{node.depth}단계 · {node.company.industry}</small></button>
    </div>
    {expanded && hasChildren && <ul>{node.children.map((child) => <TreeNode key={child.pathCompanyIds.join('>')} node={child} selectedId={selectedId} onSelect={onSelect} />)}</ul>}
  </li>
}

export function NetworkTree({ nodes, selectedId, onSelect }: { nodes: NetworkTreeNode[]; selectedId?: string; onSelect: (id: string) => void }) {
  if (nodes.length === 0) return <p className="network-empty">조건에 맞는 회사가 없습니다.</p>
  return <ul className="network-tree" aria-label="협력사 계보도">{nodes.map((node) => <TreeNode key={node.pathCompanyIds.join('>')} node={node} selectedId={selectedId} onSelect={onSelect} />)}</ul>
}
