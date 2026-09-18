import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, Plus, Sparkles } from 'lucide-react'
import { Badge } from '../../components/ui/badge'
import { Button } from '../../components/ui/button'
import { Card, CardContent } from '../../components/ui/card'
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

interface DummySlot {
  slotId: string
  date: string
  startTime: string
  endTime: string
  totalCapacity: number
  availableCapacity: number
  energyOfferedKwh: number
}

function isoDateOffset(days: number): string {
  const d = new Date()
  d.setDate(d.getDate() + days)
  return d.toISOString().slice(0, 10)
}

const INITIAL_SLOTS: DummySlot[] = [
  {
    slotId: 'SL-0001',
    date: isoDateOffset(0),
    startTime: '09:00',
    endTime: '11:00',
    totalCapacity: 4,
    availableCapacity: 3,
    energyOfferedKwh: 20,
  },
  {
    slotId: 'SL-0002',
    date: isoDateOffset(1),
    startTime: '14:00',
    endTime: '16:00',
    totalCapacity: 4,
    availableCapacity: 4,
    energyOfferedKwh: 20,
  },
  {
    slotId: 'SL-0003',
    date: isoDateOffset(2),
    startTime: '09:00',
    endTime: '10:00',
    totalCapacity: 2,
    availableCapacity: 0,
    energyOfferedKwh: 10,
  },
]

const EMPTY_FORM = { date: isoDateOffset(0), startTime: '', endTime: '', totalCapacity: '', energyOfferedKwh: '' }

/**
 * Placeholder UI only - not wired to the real /api/nodes/{nodeId}/slots endpoints yet.
 * "Add slot" and "Generate for today" mutate local state so the screen is interactive.
 */
export default function SlotManagement() {
  const { nodeId } = useParams<{ nodeId: string }>()
  const [slots, setSlots] = useState<DummySlot[]>(INITIAL_SLOTS)
  const [isDialogOpen, setDialogOpen] = useState(false)
  const [form, setForm] = useState(EMPTY_FORM)

  const nextSlotId = () => `SL-${String(slots.length + 1).padStart(4, '0')}`

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    const totalCapacity = Number(form.totalCapacity) || 0

    setSlots((prev) => [
      ...prev,
      {
        slotId: nextSlotId(),
        date: form.date,
        startTime: form.startTime,
        endTime: form.endTime,
        totalCapacity,
        availableCapacity: totalCapacity,
        energyOfferedKwh: Number(form.energyOfferedKwh) || 0,
      },
    ])

    setForm(EMPTY_FORM)
    setDialogOpen(false)
  }

  const handleGenerateForToday = () => {
    setSlots((prev) => [
      ...prev,
      {
        slotId: nextSlotId(),
        date: isoDateOffset(0),
        startTime: '08:00',
        endTime: '10:00',
        totalCapacity: 4,
        availableCapacity: 4,
        energyOfferedKwh: 20,
      },
    ])
  }

  const updateAvailableCapacity = (slotId: string, value: number) => {
    setSlots((prev) =>
      prev.map((slot) => (slot.slotId === slotId ? { ...slot, availableCapacity: value } : slot)),
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <Link
        to="/backoffice/nodes"
        className="inline-flex w-fit items-center gap-1 text-sm text-sg-primary hover:underline"
      >
        <ArrowLeft size={14} />
        Back to nodes
      </Link>

      <div className="flex items-center justify-between">
        <div>
          <h2 className="sg-h3">Slots for {nodeId}</h2>
          <p className="sg-body-sm text-sg-text-secondary">
            Bookable windows for the next 7 days. (Placeholder screen — not yet wired to the API.)
          </p>
        </div>

        <div className="flex gap-2">
          <Button variant="outline" onClick={handleGenerateForToday}>
            <Sparkles size={16} className="mr-2" />
            Generate for today
          </Button>

          <Dialog open={isDialogOpen} onOpenChange={setDialogOpen}>
            <Button onClick={() => setDialogOpen(true)}>
              <Plus size={16} className="mr-2" />
              Add slot
            </Button>

            <DialogContent>
              <form onSubmit={handleSubmit}>
                <DialogHeader>
                  <DialogTitle>Add slot</DialogTitle>
                </DialogHeader>

                <div className="flex flex-col gap-4 py-4">
                  <div className="grid grid-cols-3 gap-4">
                    <div className="flex flex-col gap-2">
                      <Label htmlFor="date">Date</Label>
                      <Input
                        id="date"
                        type="date"
                        value={form.date}
                        onChange={(e) => setForm((p) => ({ ...p, date: e.target.value }))}
                        required
                      />
                    </div>
                    <div className="flex flex-col gap-2">
                      <Label htmlFor="startTime">Start</Label>
                      <Input
                        id="startTime"
                        type="time"
                        value={form.startTime}
                        onChange={(e) => setForm((p) => ({ ...p, startTime: e.target.value }))}
                        required
                      />
                    </div>
                    <div className="flex flex-col gap-2">
                      <Label htmlFor="endTime">End</Label>
                      <Input
                        id="endTime"
                        type="time"
                        value={form.endTime}
                        onChange={(e) => setForm((p) => ({ ...p, endTime: e.target.value }))}
                        required
                      />
                    </div>
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <div className="flex flex-col gap-2">
                      <Label htmlFor="totalCapacity">Total capacity</Label>
                      <Input
                        id="totalCapacity"
                        type="number"
                        min="1"
                        value={form.totalCapacity}
                        onChange={(e) => setForm((p) => ({ ...p, totalCapacity: e.target.value }))}
                        required
                      />
                    </div>
                    <div className="flex flex-col gap-2">
                      <Label htmlFor="energyOfferedKwh">Energy offered (kWh)</Label>
                      <Input
                        id="energyOfferedKwh"
                        type="number"
                        min="0"
                        step="any"
                        value={form.energyOfferedKwh}
                        onChange={(e) => setForm((p) => ({ ...p, energyOfferedKwh: e.target.value }))}
                        required
                      />
                    </div>
                  </div>
                </div>

                <DialogFooter>
                  <Button type="submit">Create slot</Button>
                </DialogFooter>
              </form>
            </DialogContent>
          </Dialog>
        </div>
      </div>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Slot ID</TableHead>
                <TableHead>Date</TableHead>
                <TableHead>Time</TableHead>
                <TableHead>Total capacity</TableHead>
                <TableHead>Available</TableHead>
                <TableHead>Energy offered</TableHead>
                <TableHead>Status</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {slots
                .slice()
                .sort((a, b) => (a.date + a.startTime).localeCompare(b.date + b.startTime))
                .map((slot) => (
                  <TableRow key={slot.slotId}>
                    <TableCell className="font-medium">{slot.slotId}</TableCell>
                    <TableCell>{slot.date}</TableCell>
                    <TableCell>
                      {slot.startTime} - {slot.endTime}
                    </TableCell>
                    <TableCell>{slot.totalCapacity}</TableCell>
                    <TableCell>
                      <Input
                        type="number"
                        min={0}
                        max={slot.totalCapacity}
                        value={slot.availableCapacity}
                        onChange={(e) => updateAvailableCapacity(slot.slotId, Number(e.target.value))}
                        className="w-20"
                      />
                    </TableCell>
                    <TableCell>{slot.energyOfferedKwh} kWh</TableCell>
                    <TableCell>
                      <Badge variant={slot.availableCapacity > 0 ? 'default' : 'secondary'}>
                        {slot.availableCapacity > 0 ? 'Bookable' : 'Full'}
                      </Badge>
                    </TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  )
}
