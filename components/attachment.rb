require 'components/gridobject'

module Cpg::Components
	class Attachment < GridObject
		attr_reader :objects
		attr_accessor :offset
	
		def initialize(objs)
			super()

			@objects = objs ? objs.dup : []
			@offset = 0
		end
	
		def self.limits
			[-1, -1]
		end
	
		def allocation
			Cpg::Allocation.new([0, 0, 0, 0])
		end
	
		def hittest(rect, factor)
			false
		end
		
		def <<(obj)
			[obj].flatten.each do |o|
				if self.class.limits[1] == -1 || @objects.length < self.class.limits[1]
					@objects << o
					obj.link(self)
				end
			end
		end
		
		def delete(obj)
			if @objects.include?(obj)
				@objects.delete(obj)
				obj.unlink(self)
			end
		end
	
		def same_objects(other)
			other.objects.each_index do |i|
				return false if @objects[i] != other.objects[i]
			end
		
			true
		end
	
		def removed
		end
	
		def simulation_reset
		end
	
		def simulation_evaluate
			{}
		end
	end
end
