import { useState } from 'react'
import type { FormEvent } from 'react'
import { Plus, Zap } from 'lucide-react'
import { Badge } from '../../components/ui/badge'
import { Button } from '../../components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '../../components/ui/dialog'
import { Input } from '../../components/ui/input'
import { Label } from '../../components/ui/label'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '../../components/ui/table'

interface DummyNode {
  nodeId: string
  name: string
  addressLine: string
  capacityKwh: number
  batterySlotCount: number
  type: 'AC' | 'DC'
  isActive: boolean
}

const INITIAL_NODES: DummyNode[] = [
  {
    nodeId: 'ND-0001',
    name: 'Moratuwa Microgrid Hub A',
    addressLine: 'Katubedda, Moratuwa',
    capacityKwh: 250,
    batterySlotCount: 8,
    type: 'AC',
    isActive: true,
  },
  {
    nodeId: 'ND-0002',
    name: 'Colombo 07 Hub',
    addressLine: 'Cinnamon Gardens, Colombo 07',
    capacityKwh: 180,
    batterySlotCount: 6,
    type: 'DC',
    isActive: true,
  },
  {
    nodeId: 'ND-0003',
    name: 'Kadawatha Hub',
    addressLine: 'Kadawatha Junction',
    capacityKwh: 120,
    batterySlotCount: 4,
    type: 'AC',
    isActive: false,
  },
]

const EMPTY_FORM = { name: '', addressLine: '', capacityKwh: '', batterySlotCount: '', type: 'AC' as const }

/**
 * Placeholder UI only - not wired to the real /api/nodes endpoints yet.
 * "Add node" appends to local state so the screen still feels interactive.
 */
export default function NodeManagement() {
  const [nodes, setNodes] = useState<DummyNode[]>(INITIAL_NODES)
  const [isDialogOpen, setDialogOpen] = useState(false)
  const [form, setForm] = useState(EMPTY_FORM)

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()

    const nextId = `ND-${String(nodes.length + 1).padStart(4, '0')}`
    setNodes((prev) => [
      ...prev,
      {
        nodeId: nextId,
        name: form.name,
        addressLine: form.addressLine,
        capacityKwh: Number(form.capacityKwh) || 0,
        batterySlotCount: Number(form.batterySlotCount) || 0,
        type: form.type,
        isActive: true,
      },
    ])

    setForm(EMPTY_FORM)
    setDialogOpen(false)
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="sg-h3">Microgrid nodes</h2>
          <p className="sg-body-sm text-sg-text-secondary">
            Solar hubs registered across the grid. (Placeholder screen — not yet wired to the API.)
          </p>
        </div>

        <Dialog open={isDialogOpen} onOpenChange={setDialogOpen}>
          <Button onClick={() => setDialogOpen(true)}>
            <Plus size={16} className="mr-2" />
            Add node
          </Button>

          <DialogContent>
            <form onSubmit={handleSubmit}>
              <DialogHeader>
                <DialogTitle>Add node</DialogTitle>
              </DialogHeader>

              <div className="flex flex-col gap-4 py-4">
                <div className="flex flex-col gap-2">
                  <Label htmlFor="name">Name</Label>
                  <Input
                    id="name"
                    value={form.name}
                    onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
                    required
                  />
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="addressLine">Address</Label>
                  <Input
                    id="addressLine"
                    value={form.addressLine}
                    onChange={(e) => setForm((p) => ({ ...p, addressLine: e.target.value }))}
                    required
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="flex flex-col gap-2">
                    <Label htmlFor="capacityKwh">Capacity (kWh)</Label>
                    <Input
                      id="capacityKwh"
                      type="number"
                      value={form.capacityKwh}
                      onChange={(e) => setForm((p) => ({ ...p, capacityKwh: e.target.value }))}
                      required
                    />
                  </div>
                  <div className="flex flex-col gap-2">
                    <Label htmlFor="batterySlotCount">Battery slots</Label>
                    <Input
                      id="batterySlotCount"
                      type="number"
                      value={form.batterySlotCount}
                      onChange={(e) => setForm((p) => ({ ...p, batterySlotCount: e.target.value }))}
                      required
                    />
                  </div>
                </div>
              </div>

              <DialogFooter>
                <Button type="submit">Create node</Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {nodes.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-sg-border py-16 text-center">
          <Zap size={28} className="text-sg-text-muted" />
          <p className="sg-h4">No nodes yet</p>
          <p className="sg-body-sm text-sg-text-secondary">Registered microgrid hubs will appear here.</p>
        </div>
      ) : (
        <div className="rounded-lg border border-sg-border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Node ID</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Location</TableHead>
                <TableHead>Capacity</TableHead>
                <TableHead>Battery slots</TableHead>
                <TableHead>Type</TableHead>
                <TableHead>Status</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {nodes.map((node) => (
                <TableRow key={node.nodeId}>
                  <TableCell className="font-medium">{node.nodeId}</TableCell>
                  <TableCell>{node.name}</TableCell>
                  <TableCell>{node.addressLine}</TableCell>
                  <TableCell>{node.capacityKwh} kWh</TableCell>
                  <TableCell>{node.batterySlotCount}</TableCell>
                  <TableCell>{node.type}</TableCell>
                  <TableCell>
                    <Badge variant={node.isActive ? 'default' : 'secondary'}>
                      {node.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}
