import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { BatteryCharging, CalendarClock, MapPin, Plus, Radio, Zap } from 'lucide-react'
import { Button } from '../../components/ui/button'
import { Card, CardContent, CardFooter, CardHeader } from '../../components/ui/card'
import { Input } from '../../components/ui/input'
import { Label } from '../../components/ui/label'
import {
  Sheet,
  SheetBody,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from '../../components/ui/sheet'

interface DummyNode {
  nodeId: string
  name: string
  addressLine: string
  capacityKwh: number
  batterySlotCount: number
  type: 'AC' | 'DC'
  isActive: boolean
}

const EMPTY_FORM = { name: '', addressLine: '', capacityKwh: '', batterySlotCount: '', type: 'AC' as const }

/**
 * Placeholder UI only - not wired to the real /api/nodes endpoints yet.
 * "Add node" appends to local state so the screen still feels interactive.
 */
export default function NodeManagement() {
  const [nodes, setNodes] = useState<DummyNode[]>([])
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

        <Button onClick={() => setDialogOpen(true)}>
          <Plus size={16} className="mr-2" />
          Add node
        </Button>

        <Sheet open={isDialogOpen} onOpenChange={setDialogOpen}>
          <SheetContent className="p-0">
            <form onSubmit={handleSubmit} className="flex h-full flex-col">
              <SheetHeader>
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary">
                    <Radio size={18} />
                  </div>
                  <div>
                    <SheetTitle>Add node</SheetTitle>
                    <SheetDescription>Register a new microgrid hub.</SheetDescription>
                  </div>
                </div>
              </SheetHeader>

              <SheetBody className="flex flex-col gap-5">
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
              </SheetBody>

              <SheetFooter>
                <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
                  Cancel
                </Button>
                <Button type="submit">Create node</Button>
              </SheetFooter>
            </form>
          </SheetContent>
        </Sheet>
      </div>

      {nodes.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-sg-border py-16 text-center">
          <Zap size={28} className="text-sg-text-muted" />
          <p className="sg-h4">No nodes yet</p>
          <p className="sg-body-sm text-sg-text-secondary">Registered microgrid hubs will appear here.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {nodes.map((node) => (
            <Card key={node.nodeId} className="flex flex-col overflow-hidden">
              <CardHeader className="flex-row items-start justify-between gap-3 space-y-0 border-b border-sg-border bg-sg-surface-muted/60 pb-4">
                <div className="flex items-start gap-3">
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary">
                    <Radio size={18} />
                  </div>
                  <div className="min-w-0">
                    <p className="sg-h4 truncate">{node.name}</p>
                    <p className="sg-caption font-mono">{node.nodeId}</p>
                  </div>
                </div>
                <span className={`sg-badge shrink-0 ${node.isActive ? 'sg-badge-success' : 'sg-badge-neutral'}`}>
                  <span className="sg-status-dot" />
                  {node.isActive ? 'Active' : 'Inactive'}
                </span>
              </CardHeader>

              <CardContent className="flex flex-1 flex-col gap-4 pt-4">
                <div className="flex items-start gap-2">
                  <MapPin size={15} className="mt-0.5 shrink-0 text-sg-text-muted" />
                  <p className="sg-body-sm">{node.addressLine}</p>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div className="rounded-md border border-sg-border p-3">
                    <div className="flex items-center gap-1.5 text-sg-text-muted">
                      <Zap size={13} />
                      <p className="sg-caption">Capacity</p>
                    </div>
                    <p className="mt-1 text-lg font-semibold text-sg-text">
                      {node.capacityKwh}
                      <span className="ml-1 text-xs font-normal text-sg-text-muted">kWh</span>
                    </p>
                  </div>
                  <div className="rounded-md border border-sg-border p-3">
                    <div className="flex items-center gap-1.5 text-sg-text-muted">
                      <BatteryCharging size={13} />
                      <p className="sg-caption">Battery slots</p>
                    </div>
                    <p className="mt-1 text-lg font-semibold text-sg-text">{node.batterySlotCount}</p>
                  </div>
                </div>

                <span className={`sg-badge w-fit ${node.type === 'AC' ? 'sg-badge-info' : 'sg-badge-primary'}`}>
                  {node.type} node
                </span>
              </CardContent>

              <CardFooter className="border-t border-sg-border pt-4">
                <Link to={`/backoffice/nodes/${node.nodeId}/slots`} className="sg-btn sg-btn-ghost sg-btn-sm w-full">
                  <CalendarClock size={14} />
                  View slots
                </Link>
              </CardFooter>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
